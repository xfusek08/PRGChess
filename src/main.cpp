
#include <stdexcept>
#include <iostream>

#include <RenderBase/rb.h>

using namespace std;
using namespace rb;

class App : public Application
{
    using Application::Application;

    GLuint vao;
    GLuint prg;

    bool init() {

        glClearColor(0, 0, 0, 1);
        glCreateVertexArrays(1, &vao);
        prg = this->graphics->createProgram(
            this->graphics->createShader(GL_VERTEX_SHADER, SHADER_VERTEX),
            this->graphics->createShader(GL_FRAGMENT_SHADER, SHADER_FRAGMENT)
        );

        this->mainWindow->onFPSCount([=](FrameStat frameStat) {
            cout << "Fps: " << frameStat.frames << "\n";
        });

        auto resUniform = glGetUniformLocation(prg, "resolution");

        glUniform2ui(resUniform, this->mainWindow->getWidth(), this->mainWindow->getHeight());

        return true;
    }

    bool update(const Event &event) {
        // todo on resize set screen size uniform
        return true;
    }

    void draw() {
        glClear(GL_COLOR_BUFFER_BIT);
        glPointSize(10);
        glBindVertexArray(vao);
        glUseProgram(prg);
        glDrawArrays(GL_TRIANGLES,0,6);
    }
};

int main(int argc, char *argv[]) {
    auto app = App(Configuration(argc, argv));
    return app.run();
}
