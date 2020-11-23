
#include <stdexcept>
#include <iostream>
#include <array>

// #define DISABLE_LOGGING

#include <RenderBase/rb.h>
#include <RenderBase/tools/logging.h>
#include <RenderBase/tools/camera.h>

#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include <scene/Scene.h>

#include <sceneGpuUtils.h>

using namespace std;
using namespace rb;

class App : public Application
{
    using Application::Application;

    // my objects
    unique_ptr<OrbitCameraController> orbitCamera;
    unique_ptr<Scene> scene;

    // gl stuff
    GLuint vao;
    unique_ptr<Program> prg;
    unique_ptr<UniformBuffer> sceneBuffer;

    unique_ptr<UniformBuffer> primitiveBuffer;
    unique_ptr<UniformBuffer> materialBuffer;
    unique_ptr<UniformBuffer> modelBuffer;

    bool init() {

        // performance setup
        this->mainWindow->getPerformanceAnalyzer()->capFPS(24);
        this->mainWindow->getPerformanceAnalyzer()->perPeriodReport(1s, [=](IntervalPerformanceReport report) {
            cout << "fps: " << report.frames << "\n";
            cout << "Average frame duration: " << report.averageFrameTime.count() << " us\n";
            cout << "Longest frame: " << report.maxFrameTime.count() << " us\n";
            cout << "shortest frame: " << report.minFrameTime.count() << " us\n";
        });

        // gl program setup
        glClearColor(0, 0, 0, 1);
        glCreateVertexArrays(1, &vao);
        prg = make_unique<Program>(
            make_shared<Shader>(GL_VERTEX_SHADER, SHADER_VERTEX),
            make_shared<Shader>(GL_FRAGMENT_SHADER, SHADER_PRIMITIVE_SDF),
            make_shared<Shader>(GL_FRAGMENT_SHADER, SHADER_FRAGMENT)
        );
        if (!prg->getErrorMessage().empty()) {
            cerr << "Error while creating a program: \n" << prg->getErrorMessage() << endl;
            return false;
        }

        // camera setup
        auto cam = make_shared<Camera>(glm::vec3(0, 1, 0));
        cam->setFov(glm::radians(60.0f));
        cam->setAspectRatio(float(mainWindow->getWidth()) / float(mainWindow->getHeight()));
        cam->setPosition(glm::vec3(0, 10, -10));
        cam->setTargetPosition(glm::vec3(0, 0, 0));
        orbitCamera = make_unique<OrbitCameraController>(cam);
        updateCamera();

        // scene setup
        prg->uniform("lightPosition", glm::vec3(-10, 20, 5)); // in the future make light part of the scene
        buildScene();
        updateScene();

        return true;
    }

    bool update(const Event &event) {
        if (orbitCamera->processEvent(event)) {
            updateCamera();
        }
        return true;
    }

    void draw() {
        prg->use();
        glClear(GL_COLOR_BUFFER_BIT);
        glBindVertexArray(vao);
        glDrawArrays(GL_TRIANGLES,0,6);
    }

    // loads camera dat to GPU
    void updateCamera() {
        LOG_DEBUG("Position:         " << glm::to_string(orbitCamera->camera->getPosition()));
        LOG_DEBUG("Target:           " << glm::to_string(orbitCamera->camera->getTargetPosition()));
        LOG_DEBUG("Direction:        " << glm::to_string(orbitCamera->camera->getDirection()));
        LOG_DEBUG("cameraFOVDegrees: " << glm::degrees(orbitCamera->camera->getFov()));
        LOG_DEBUG("frustumCorners:   " << glm::to_string(orbitCamera->camera->getFrustumCorners()));

        float fovTangent = glm::tan(orbitCamera->camera->getFov() / 2.0f);
        prg->uniform("cameraPosition",    orbitCamera->camera->getPosition());
        prg->uniform("cameraDirection",   orbitCamera->camera->getDirection());
        prg->uniform("upRayDistorsion",   orbitCamera->camera->getOrientationUp()   * fovTangent);
        prg->uniform("leftRayDistorsion", orbitCamera->camera->getOrientationLeft() * fovTangent * orbitCamera->camera->getAspectRatio());
    }

    // loads scene data to GPU
    void updateScene() {
        auto shaderData = prepareShaderSceneData(*scene);

        primitiveBuffer = make_unique<UniformBuffer>(shaderData->primitives);
        materialBuffer  = make_unique<UniformBuffer>(shaderData->materials);
        modelBuffer     = make_unique<UniformBuffer>(shaderData->models);

        prg->uniform("PrimitivesBlock", *primitiveBuffer);
        prg->uniform("MaterialBlock", *materialBuffer, 1);
        prg->uniform("ModelsBlock", *modelBuffer, 2);
        prg->uniform("modelsTotal", glm::u32(shaderData->models.size()));
    }

    void buildScene() {
        scene = make_unique<Scene>();

        // Register materials

        auto whiteClay = Material();
        whiteClay.color = {1.0f, 1.0f, 0.8f};
        whiteClay.shininess = 500.0f;
        scene->materials["whiteClay"] = whiteClay;

        auto redClay = Material();
        redClay.color = {0.7f, 0.2f, 0.2f};
        redClay.shininess = 5.0f;
        scene->materials["redClay"] = redClay;

        auto blueClay = Material();
        blueClay.color = {0.2f, 0.3f, 0.8f};
        blueClay.shininess = 100.0f;
        scene->materials["blueClay"] = blueClay;

        // Register geometries

        {   // floor geometry
            Primitive box = { PrimitiveType::Box };
            box.data = {7.0f, 1.0f, 7.0f, 0.03f};
            Primitive edge1 = { PrimitiveType::Cilinder };
            edge1.transform.translate({ 0.0f, 0.5f, 7.0f });
            edge1.transform.rotate({ 0.0f, 0.0f, 90.0f });
            edge1.data.x   = 0.2f;
            edge1.data.y   = 8.0f;
            edge1.blending = 0.03f;
            edge1.operation = PrimitiveOperation::Substract;
            scene->geometries["floor"] = ModelGeometry(box, edge1);
        }

        {   // Figure
            Primitive body = { PrimitiveType::Cilinder };
            body.transform.translate({ 0.0f, 1.0f, 0.0f });
            body.data.x = 0.35f;
            body.data.y = 1.0f;

            Primitive head = { PrimitiveType::Sphere };
            head.transform.translate({ 0.0f, 2.4f, 0.0f });
            head.data.x = 0.8f;
            head.blending = 0.1f;

            Primitive base = { PrimitiveType::Torus };
            base.data.x = 0.9f;
            base.data.y = 0.5f;
            base.blending = 0.8f;

            Primitive flatBottom = { PrimitiveType::Box };
            flatBottom.transform.translate({ 0.0f, -0.5f, 0.0f });
            flatBottom.data = {1.5f, 0.2f, 1.5f, 0.0f};
            flatBottom.operation = PrimitiveOperation::Substract;

            scene->geometries["figure"] = ModelGeometry(body, head, base, flatBottom);
        }

        // { // reference geometry
        //     scene->geometries["reference"] = ModelGeometry(
        //         Primitive{ PrimitiveType::Sphere,   { {0.0f, 0.0f, 0.0f } }, { 0.5f, 0.0f, 0.0f, 0.0f } }
        //     );
        // }

        // { // test geometry
        //     scene->geometries["test"] = ModelGeometry(
        //         Primitive{ PrimitiveType::Cilinder, { {0.0f, 1.0f, 0.0f } }, { 0.4f, 1.0f, 0.0f, 0.0f } },
        //         Primitive{ PrimitiveType::Torus,    { {0.0f, 0.0f, 0.0f } }, { 0.7f, 0.4f, 0.0f, 0.0f } }
        //     );
        //     scene->geometries["test"].primitives[1].blending = 0.7;
        // }

        // Register models

        Model floor = {
            Transform({0.0f, -1.0f, 0.0f}),
            "floor",
            "whiteClay"
        };
        scene->models.push_back(floor);

        Model figure = {
            Transform({-2.0f, 3.0f, 1.0f}, {0.0f, 0.0f, 0.0f}),
            "figure",
            "redClay"
        };

        scene->models.push_back(figure);

        Model blueFig = {
            Transform({1.0f, 3.0f, -4.0f}, { 0.0f, 0.0f, 0.0f }),
            "figure",
            "blueClay"
        };
        scene->models.push_back(blueFig);

        // Model test      = { Transform(), "test", "redClay" };
        // Model reference = { Transform({ 2.0f, 0.0f, 0.0f }), "reference", "blueClay" };
        // scene->models.push_back(test);
        // scene->models.push_back(reference);
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
