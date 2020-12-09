
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

#include <sceneUtils.h>

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

    // scene gl data
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

        updateScene();
        return true;
    }

    bool update(const Event &event) {
        if (orbitCamera->processEvent(event)) {
            updateCamera();
        } else if (event.type == EventType::KeyPressed) {
            if (event.keyPressedData.keyCode == SDLK_ESCAPE) {
                exit();
            }
            if (event.keyPressedData.keyCode == SDLK_r) {
                updateScene();
            }
        }
        return true;
    }

    void draw() {
        prg->use();
        glClear(GL_COLOR_BUFFER_BIT);
        glBindVertexArray(vao);
        glDrawArrays(GL_TRIANGLES,0,6);
    }

    // loads scene data to GPU
    void updateScene() {

        prg = make_unique<Program>(
            make_shared<Shader>(GL_VERTEX_SHADER, RESOURCE_SHADERS_VERTEX),
            make_shared<Shader>(GL_FRAGMENT_SHADER, RESOURCE_SHADERS_PRIMITIVE_SDF),
            make_shared<Shader>(GL_FRAGMENT_SHADER, RESOURCE_SHADERS_FRAGMENT)
        );

        if (!prg->getErrorMessage().empty()) {
            cerr << "Error while creating a program: \n" << prg->getErrorMessage() << endl;
            return;
        }

        // camera setup
        auto camPos     = glm::vec3(0, 10, -10);
        auto camTarget  = glm::vec3(0, 0, 0);
        if (orbitCamera != nullptr) {
            camPos    = orbitCamera->camera->getPosition();
            camTarget = orbitCamera->camera->getTargetPosition();
        }
        auto cam = make_shared<Camera>(glm::vec3(0, 1, 0));
        cam->setFov(glm::radians(60.0f));
        cam->setAspectRatio(float(mainWindow->getWidth()) / float(mainWindow->getHeight()));
        cam->setPosition(camPos);
        cam->setTargetPosition(camTarget);
        orbitCamera = make_unique<OrbitCameraController>(cam);
        updateCamera();

        prg->uniform("lightPosition", glm::vec3(10, 15, 10)); // in the future make light part of the scene

        scene = buildSceneFromJson(RESOURCE_SCENE);

        auto shaderData = prepareShaderSceneData(*scene);

        primitiveBuffer = make_unique<UniformBuffer>(shaderData.primitives);
        materialBuffer  = make_unique<UniformBuffer>(shaderData.materials);
        modelBuffer     = make_unique<UniformBuffer>(shaderData.models);

        prg->uniform("PrimitivesBlock", *primitiveBuffer);
        prg->uniform("MaterialBlock", *materialBuffer, 1);
        prg->uniform("ModelsBlock", *modelBuffer, 2);
        prg->uniform("modelsTotal", glm::u32(shaderData.models.size()));
    }

    // loads camera dat to GPU
    void updateCamera() {
        LOG_DEBUG("Position:         " << glm::to_string(orbitCamera->camera->getPosition()));
        LOG_DEBUG("Target:           " << glm::to_string(orbitCamera->camera->getTargetPosition()));
        LOG_DEBUG("Direction:        " << glm::to_string(orbitCamera->camera->getDirection()));
        LOG_DEBUG("cameraFOVDegrees: " << glm::degrees(orbitCamera->camera->getFov()));

        float fovTangent = glm::tan(orbitCamera->camera->getFov() / 2.0f);
        prg->uniform("cameraPosition",    orbitCamera->camera->getPosition());
        prg->uniform("cameraDirection",   orbitCamera->camera->getDirection());
        prg->uniform("upRayDistorsion",   orbitCamera->camera->getOrientationUp()   * fovTangent);
        prg->uniform("leftRayDistorsion", orbitCamera->camera->getOrientationLeft() * fovTangent * orbitCamera->camera->getAspectRatio());
    }

};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
