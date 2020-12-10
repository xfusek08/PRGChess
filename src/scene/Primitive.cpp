
#include <scene/Primitive.h>
#include <RenderBase/tools/utils.h>
#include <string>

using namespace std;

#define CHECK_SET_PROPERTY(propName) if (rb::utils::toLower(name) == rb::utils::toLower(#propName))  set##propName(value);

PrimitiveType Primitive::typeFromString(const std::string& name) {
    return rb::utils::getOr(primitiveTypeDict, name, PrimitiveType::ptInvalid);
}

PrimitiveOperation Primitive::operationFromString(const std::string& name) {
    return rb::utils::getOr(primitiveoperationDict, name, PrimitiveOperation::poInvalid);
}

shared_ptr<Primitive> Primitive::createByType(PrimitiveType type) {
    switch (type) {
        case PrimitiveType::ptSphere:    return make_shared<Sphere>();
        case PrimitiveType::ptCapsule:   return make_shared<Capsule>();
        case PrimitiveType::ptTorus:     return make_shared<Torus>();
        case PrimitiveType::ptBox:       return make_shared<Box>();
        case PrimitiveType::ptCilinder:  return make_shared<Cilinder>();
        case PrimitiveType::ptCone:      return make_shared<Cone>();
        case PrimitiveType::ptRoundCone: return make_shared<RoundCone>();
        default: return make_shared<Primitive>();
    }
}

// Primitive

PrimitiveType Primitive::getType() const {
    return PrimitiveType::ptInvalid;
}

glm::vec3 Primitive::getDimensions() const {
    return glm::vec3();
}


// Sphere

PrimitiveType Sphere::getType() const { return PrimitiveType::ptSphere; }

glm::vec3 Sphere::getDimensions() const {
    return 2.0f * glm::vec3(data.x, data.x, data.x) + (blending * 0.5f);
}

void Sphere::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(Radius)
    else CHECK_SET_PROPERTY(Diameter)
}


// Capsule

PrimitiveType Capsule::getType() const { return PrimitiveType::ptCapsule; }

glm::vec3 Capsule::getDimensions() const {
    return glm::vec3( 2.0f * data.x, data.y + 2.0f * data.x, 2.0f * data.x) + (blending * 0.5f);
}

void Capsule::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(HalfWidth)
    else CHECK_SET_PROPERTY(HalfHeight)
    else CHECK_SET_PROPERTY(Radius)
    else CHECK_SET_PROPERTY(Width)
    else CHECK_SET_PROPERTY(Height)
    else CHECK_SET_PROPERTY(Diameter)
}


// Torus

PrimitiveType Torus::getType() const { return PrimitiveType::ptTorus; }

glm::vec3 Torus::getDimensions() const {
    return 2.0f * glm::vec3( data.x + data.y, data.y, data.x + data.y) + (blending * 0.5f);
}

void Torus::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(RadiusInner)
    else CHECK_SET_PROPERTY(RadiusOuther)
    else CHECK_SET_PROPERTY(DiameterInner)
    else CHECK_SET_PROPERTY(DiameterOuther)
}


// Box

PrimitiveType Box::getType() const { return PrimitiveType::ptBox; }

glm::vec3 Box::getDimensions() const {
    return 2.0f * glm::vec3(data.x, data.y, data.z) + (blending * 0.5f);
}

void Box::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(HalfWidth)
    else CHECK_SET_PROPERTY(HalfHeight)
    else CHECK_SET_PROPERTY(HalfDepth)
    else CHECK_SET_PROPERTY(Rounding)
    else CHECK_SET_PROPERTY(Width)
    else CHECK_SET_PROPERTY(Height)
    else CHECK_SET_PROPERTY(Depth)
}


// Cilinder

PrimitiveType Cilinder::getType() const { return PrimitiveType::ptCilinder; }

glm::vec3 Cilinder::getDimensions() const {
    return 2.0f * glm::vec3( data.x, data.y, data.x) + (blending * 0.5f);
}

void Cilinder::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(Radius)
    else CHECK_SET_PROPERTY(HalfHeight)
    else CHECK_SET_PROPERTY(Rounding)
    else CHECK_SET_PROPERTY(Width)
    else CHECK_SET_PROPERTY(Diameter)
    else CHECK_SET_PROPERTY(Height)
}


// Cone

PrimitiveType Cone::getType() const { return PrimitiveType::ptCone; }

glm::vec3 Cone::getDimensions() const {
    return 2.0f * glm::vec3( glm::max(data.x, data.y), data.z, glm::max(data.x, data.y)) + (blending * 0.5f);
}

void Cone::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(RadiusTop)
    else CHECK_SET_PROPERTY(RadiusBottom)
    else CHECK_SET_PROPERTY(HalfHeight)
    else CHECK_SET_PROPERTY(Rounding)
    else CHECK_SET_PROPERTY(DiameterTop)
    else CHECK_SET_PROPERTY(DiameterBottom)
    else CHECK_SET_PROPERTY(Height)
}


// RoundCone

PrimitiveType RoundCone::getType() const { return PrimitiveType::ptRoundCone; }

glm::vec3 RoundCone::getDimensions() const {
    return glm::vec3( 2.0f * glm::max(data.x, data.y), data.z + data.x + data.y, 2.0f * glm::max(data.x, data.y)) + (blending * 0.5f);
}

void RoundCone::setDataPropertyByName(const std::string& name, float value) {
    CHECK_SET_PROPERTY(RadiusTop)
    else CHECK_SET_PROPERTY(RadiusBottom)
    else CHECK_SET_PROPERTY(HalfHeight)
    else CHECK_SET_PROPERTY(DiameterTop)
    else CHECK_SET_PROPERTY(DiameterBottom)
    else CHECK_SET_PROPERTY(Height)
}
