#pragma once

#include <scene/Transform.h>

#include <unordered_map>

enum PrimitiveType {
    Sphere    = 0,
    Capsule   = 1,
    Torus     = 2,
    Box       = 3,
    Cilinder  = 4,
    Cone      = 5,
    RoundCone = 6,
    ptInvalid = 100,
};

enum PrimitiveOperation {
    Add       = 0,
    Substract = 1,
    Intersect = 2,
    poInvalid = 100,
};

static const std::unordered_map<std::string, PrimitiveType> primitiveTypeDict = {
    {"sphere",    PrimitiveType::Sphere},
    {"capsule",   PrimitiveType::Capsule},
    {"torus",     PrimitiveType::Torus},
    {"box",       PrimitiveType::Box},
    {"cilinder",  PrimitiveType::Cilinder},
    {"ciln",      PrimitiveType::Cilinder},
    {"cone",      PrimitiveType::Cone},
    {"roundcone", PrimitiveType::RoundCone},
    {"rcone",     PrimitiveType::RoundCone},
};


static const std::unordered_map<std::string, PrimitiveOperation> primitiveoperationDict = {
    {"add",       PrimitiveOperation::Add},
    {"substract", PrimitiveOperation::Substract},
    {"sub",       PrimitiveOperation::Substract},
    {"intersect", PrimitiveOperation::Intersect},
    {"isect",     PrimitiveOperation::Intersect},
};

struct Primitive
{
    PrimitiveType      type      = PrimitiveType::ptInvalid;
    Transform          transform = Transform();
    glm::vec4          data      = glm::vec4(0);
    PrimitiveOperation operation = PrimitiveOperation::Add;
    float              blending  = 0.0f;

    glm::vec3 getDimensions() const;
    glm::vec3 getCenter() const;

    static PrimitiveType      typeFromString(const std::string& name);
    static PrimitiveOperation operationFromString(const std::string& name);
};
