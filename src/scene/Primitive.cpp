
#include <scene/Primitive.h>

glm::vec3 Primitive::getDimensions() const {
    switch (type)
    {
        case PrimitiveType::Sphere:   return 2.0f * glm::vec3(data.x, data.x, data.x);
        case PrimitiveType::Capsule:  return glm::vec3( 2.0f * data.x, data.y + 2.0f * data.x, 2.0f * data.x) + blending;
        case PrimitiveType::Torus:    return 2.0f * glm::vec3( data.x + data.y, data.y, data.x + data.y);
        case PrimitiveType::Box:      return 2.0f * glm::vec3(data.x, data.y, data.z);
        case PrimitiveType::Cilinder: return 2.0f * glm::vec3( data.x, data.y, data.x) + blending;
    }
    return glm::vec3(0);
}
