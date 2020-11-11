
#include <stdexcept>
#include <iostream>

#include <RenderBase/rb.h>

#include <RenderBase/logging.h>

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

        this->mainWindow->onFPSCount([=](FrameStat frameStat) {
            cout << "Fps: " << frameStat.frames << "\n";
        });

        updateCamera();

        prg->use();

        return true;
    }

    bool update(const Event &event) {
        // todo on resize set screen size uniform
        if (event.type == EventType::MouseMove && event.mouseMoveData.buttons.left) {
            cameraRotX -= event.mouseMoveData.yMovedRel;
            cameraRotY += event.mouseMoveData.xMovedRel;
            updateCamera();
        }
        return true;
    }

    void draw() {
        glClear(GL_COLOR_BUFFER_BIT);
        glPointSize(10);
        glBindVertexArray(vao);
        glDrawArrays(GL_TRIANGLES,0,6);
    }

    void updateCamera() {
        glm::vec3 cameraPosition    = glm::vec3(0, 1, 0);
        float aspectRatio           = float(this->mainWindow->getWidth()) / float(this->mainWindow->getHeight());
        glm::mat4 cameraOrientation = glm::rotate(
            glm::rotate(
                glm::mat4(1),
                cameraRotX, glm::vec3(1,0,0)
            ),
            cameraRotY, glm::vec3(0,1,0)
        );

        prg->uniform("aspectRatio",       aspectRatio);
        prg->uniform("cameraPosition",    cameraPosition);
        prg->uniform("cameraOrientation", cameraOrientation);
        prg->uniform("viewDistance",      -5.0f);
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
