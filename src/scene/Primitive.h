#pragma once

#include <scene/Transform.h>

enum PrimitiveType {
    Sphere   = 0,
    Capsule  = 1,
    Torus    = 2,
    Box      = 3,
    Cilinder = 4,
};

enum PrimitiveOperation {
    Add       = 0,
    Substract = 1,
    Intersect = 2,
};

struct Primitive
{
    PrimitiveType      type;
    Transform          transform = Transform();
    glm::vec4          data      = glm::vec4(0);
    PrimitiveOperation operation = PrimitiveOperation::Add;
    float              blending  = 0.0f;

    glm::vec3 getDimensions() const;
    glm::vec3 getCenter() const;
};
