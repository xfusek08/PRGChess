
#include <stdexcept>
#include <iostream>

#include <RenderBase/rb.h>

#include <RenderBase/tools/logging.h>
#include <RenderBase/tools/camera.h>

#include <glm/gtc/matrix_transform.hpp>

using namespace std;
using namespace rb;

class App : public Application
{
    using Application::Application;

    GLuint vao;
    unique_ptr<Program> prg;

    float cameraRotX = 0.0f;
    float cameraRotY = 0.0f;

    unique_ptr<OrbitCameraController> orbitCamera;

    bool init() {

        glClearColor(0, 0, 0, 1);
        glCreateVertexArrays(1, &vao);

        prg = make_unique<Program>(
            make_shared<Shader>(GL_VERTEX_SHADER, SHADER_VERTEX),
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
        cam->setAspectRatio(float(mainWindow->getWidth()) / float(mainWindow->getHeight()));
        cam->setPosition(glm::vec3(0, 5, -5));
        cam->setTargetPosition(glm::vec3(0, 1, 0));
        orbitCamera = make_unique<OrbitCameraController>(cam);
        updateCamera();

        // basic scene
        prg->uniform("sphere", glm::vec4(0, 1, 0, 1));
        prg->uniform("lightPosition", glm::vec3(1, 5, -1));
        prg->uniform("planeHeight", 0.0f);

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
        glPointSize(10);
        glBindVertexArray(vao);
        glDrawArrays(GL_TRIANGLES,0,6);
    }

    void updateCamera() {
        // LOG_DEBUG("Position:         " << glm::to_string(orbitCamera->camera->getPosition()));
        // LOG_DEBUG("Target:           " << glm::to_string(orbitCamera->camera->getTargetPosition()));
        // LOG_DEBUG("Direction:        " << glm::to_string(orbitCamera->camera->getDirection()));
        // LOG_DEBUG("cameraFOVDegrees: " << orbitCamera->camera->getFovDegrees());


        prg->uniform("aspectRatio",      orbitCamera->camera->getAspectRatio());
        prg->uniform("cameraPosition",   orbitCamera->camera->getPosition());
        prg->uniform("cameraDirection",  orbitCamera->camera->getDirection());
        prg->uniform("cameraUp",         orbitCamera->camera->getUpVector());
        prg->uniform("cameraFOVDegrees", orbitCamera->camera->getFovDegrees());
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
