#pragma once

#include <vector>
#include <memory>

#include <glm/glm.hpp>

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

struct Transform {
    glm::vec3 position;
    glm::vec3 rotation;
    float     size;

    Transform(
        glm::vec3 position = { 0.0, 0.0, 0.0 },
        glm::vec3 rotation = { 0.0, 0.0, 0.0 },
        float size         = 1.0
    );

    glm::mat4 getTransform() const;

    // a syntax sugar
    inline void scale(float size)                    { this->size     += size; }
    inline void translate(glm::vec3 position)        { this->position += position; }
    inline void rotate(glm::vec3 rotation)           { this->rotation += rotation; }

    inline void translate(float x, float y, float z) { translate({x, y, z}); }
    inline void rotate(float x, float y, float z)    { rotate({x, y, z}); }
};

struct Primitive
{
    PrimitiveType      type;
    Transform          transform  = Transform();
    glm::vec4          data       = glm::vec4(0);
    PrimitiveOperation operation  = PrimitiveOperation::Add;
    float              blending   = 0.0f;
};

// make SoA instead of AoS ... or only transform to SoA when loading to GPU ...s
struct Model
{
    Model(std::vector<Primitive> primitives) : primitives(primitives) {}

    template <typename... ARGS>
    Model(ARGS... primitives) : primitives(std::vector<Primitive>({primitives...})) {}

    Transform transform = Transform();
    std::vector<Primitive> primitives = {};
};

class Scene {
    public:
        std::vector<std::shared_ptr<Model>> models = {}; // one siple model as vector of primitives for now
};
