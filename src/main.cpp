
#include <stdexcept>
#include <iostream>
#include <array>

#define DISABLE_LOGGING

#include <RenderBase/rb.h>
#include <RenderBase/tools/logging.h>
#include <RenderBase/tools/camera.h>

#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include <scene.h>

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
        cam->setTargetPosition(glm::vec3(0, 2, 0));
        orbitCamera = make_unique<OrbitCameraController>(cam);
        updateCamera();

        // scene setup
        prg->uniform("lightPosition", glm::vec3(-25, 50, -25)); // in the future make light part of the scene
        prg->uniform("ambientLight", glm::vec3(0.1, 0.1, 0.1)); // in the future make light part of the scene
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

        struct ShaderPrimitive {
            glm::u32  type;
            glm::u32  operation;
            glm::f32  blending;
            glm::f32  dummy;
            glm::mat4 transform;
            glm::vec4 data;
        };

        // computeData do it by visitor pattern
        vector<ShaderPrimitive> data;
        const vector<Primitive>& primitives = scene->models[0]->primitives;
        data.reserve(primitives.size());
        for (const Primitive& primitive : primitives) {
            ShaderPrimitive shaderPrimitive;
            shaderPrimitive.type      = primitive.type;
            shaderPrimitive.transform = primitive.transform.getTransform();
            shaderPrimitive.data      = primitive.data;
            shaderPrimitive.operation = primitive.operation;
            shaderPrimitive.blending  = primitive.blending;
            data.push_back(shaderPrimitive);
        }

        // load to GPU
        sceneBuffer = make_unique<UniformBuffer>();
        sceneBuffer->load(data);
        prg->uniform("sceneSize", glm::u32(data.size()));
        prg->uniform("SceneBlock", *sceneBuffer);
    }

    void buildScene() {
        scene = make_unique<Scene>();

        // may the floor be another model in the future
        Primitive floor = { PrimitiveType::Box };
        floor.data = {8.0f, 1.0f, 8.0f, 0.2f};
        floor.transform.translate({0, 1, 0});

        Primitive sphere1 = { PrimitiveType::Sphere };
        sphere1.data.x = 1.0;
        sphere1.transform.translate({0, -1, 0});

        Primitive sphere2 = { PrimitiveType::Sphere };
        sphere2.data.x = 1.0;
        sphere2.transform.translate({0, -2.4, 0});
        sphere2.blending = 0.1;
        sphere2.operation = PrimitiveOperation::Substract;

        Primitive sphere3 = { PrimitiveType::Sphere };
        sphere3.data.x = 3.0;
        sphere3.transform.translate({2.7, -2.5, 0});
        sphere3.blending = 0.1;
        sphere3.operation = PrimitiveOperation::Intersect;

        auto model = make_shared<Model>(
            sphere1,
            sphere2,
            sphere3,
            floor
        );
        scene->models.push_back(model);
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
