
#include <scene/Transform.h>

#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtx/transform.hpp>

using namespace std;

Transform::Transform(glm::vec3 position, glm::vec3 rotation, float size) :
    position(position),
    rotation(rotation),
    size(size)
{}

glm::mat4 Transform::getTransform() const {
    auto m = glm::scale(glm::mat4(1), glm::vec3(size));
    m = m * getRotationMatrix();
    m = glm::translate(m, -position);
    return m;
}

glm::mat4 Transform::getRotationMatrix() const {
    auto m = glm::mat4(1);
    m = glm::rotate(m, glm::radians(rotation.y), glm::vec3(0.0f, 1.0f, 0.0f));
    m = glm::rotate(m, glm::radians(rotation.z), glm::vec3(0.0f, 0.0f, 1.0f));
    m = glm::rotate(m, glm::radians(rotation.x), glm::vec3(1.0f, 0.0f, 0.0f));
    return m;
}
