
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

        this->mainWindow->getPerformanceAnalyzer()->perPeriodReport(1s, [=](IntervalPerformanceReport report) {
            cout << "fps: " << report.frames << "\n";
            cout << "Average frame duration: " << report.averageFrameTime.count() << " us\n";
            cout << "Longest frame: " << report.maxFrameTime.count() << " us\n";
            cout << "shortest frame: " << report.minFrameTime.count() << " us\n";
        });

        // camera setup
        auto cam = make_shared<Camera>(glm::vec3(0, 1, 0));
        cam->setFov(glm::radians(60.0f));
        cam->setAspectRatio(float(mainWindow->getWidth()) / float(mainWindow->getHeight()));
        cam->setPosition(glm::vec3(0, 10, -10));
        cam->setTargetPosition(glm::vec3(0, 0, 0));
        orbitCamera = make_unique<OrbitCameraController>(cam);
        updateCamera();

        // basic scene
        prg->uniform("lightPosition", glm::vec3(1, 10, -5)); // in the future make light part of the scene

        // basic creation of the scene with models harcoded
        buildScene();

        // loading scene to shader unifrom buffer
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
            glm::mat4 transform;
            glm::vec4 data;
            glm::u32  type;
            float dummy1; // alligment
            float dummy2;
            float dummy3;
        };

        // computeData
        vector<ShaderPrimitive> data;
        const vector<Primitive>& primitives = scene->models[0]->primitives;
        data.reserve(primitives.size());
        for (const Primitive& primitive : primitives) {
            ShaderPrimitive shaderPrimitive;
            shaderPrimitive.type      = primitive.type;
            shaderPrimitive.transform = primitive.transform.getTransform();
            shaderPrimitive.data      = primitive.data;
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

        // begin with one model
        auto model = make_shared<Model>(Transform({ 0.0f, 2.0f, 0.0f }));
        model->primitives = {
            // a box in the center with dimensions 1 x 1 x 1
            { PrimitiveType::Capsule, Transform({ 0.0f, -4.0f, 0.0f }, { 0.0, 45.0, 90.0 }), { 3.0f, 0.3f, 0.0f, 0.0f} },
            { PrimitiveType::Sphere,  Transform({ 0.0f, -1.0f, 0.0f }), { 1.0f, 0.0f, 0.0f, 0.0f} },
            { PrimitiveType::Box,     Transform({ 1.0f, -1.0f, 1.0f }, { 0.0, 45.0, 45.0 }), { 0.5f, 0.5f, 0.5f, 0.0f} },
        };

        scene->models.push_back(model);
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
