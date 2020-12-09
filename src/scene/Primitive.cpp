
#include <scene/Primitive.h>

#include <string>

#include <RenderBase/tools/utils.h>

glm::vec3 Primitive::getDimensions() const {
    switch (type)
    {
        case PrimitiveType::Sphere:    return 2.0f * glm::vec3(data.x, data.x, data.x) + (blending * 0.5f);
        case PrimitiveType::Capsule:   return glm::vec3( 2.0f * data.x, data.y + 2.0f * data.x, 2.0f * data.x) + (blending * 0.5f);
        case PrimitiveType::Torus:     return 2.0f * glm::vec3( data.x + data.y, data.y, data.x + data.y) + (blending * 0.5f);
        case PrimitiveType::Box:       return 2.0f * glm::vec3(data.x, data.y, data.z) + (blending * 0.5f);
        case PrimitiveType::Cilinder:  return 2.0f * glm::vec3( data.x, data.y, data.x) + (blending * 0.5f);
        case PrimitiveType::Cone:      return 2.0f * glm::vec3( glm::max(data.x, data.y), data.z, glm::max(data.x, data.y)) + (blending * 0.5f);
        case PrimitiveType::RoundCone: return glm::vec3( 2.0f * glm::max(data.x, data.y), data.z + data.x + data.y, 2.0f * glm::max(data.x, data.y)) + (blending * 0.5f);
    }
    return glm::vec3(0);
}

PrimitiveType Primitive::typeFromString(const std::string& name) {
    return rb::utils::getOr(primitiveTypeDict, name, PrimitiveType::ptInvalid);
}

PrimitiveOperation Primitive::operationFromString(const std::string& name) {
    return rb::utils::getOr(primitiveoperationDict, name, PrimitiveOperation::poInvalid);
}
