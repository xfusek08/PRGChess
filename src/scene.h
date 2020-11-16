#pragma once

#include <vector>
#include <memory>

#include <glm/glm.hpp>

enum PrimitiveType {
    Sphere = 0,
};

class SceneObject
{
    public:
        glm::vec3 position = glm::vec3(0);
};

class Primitive : public SceneObject
{
    public:
        PrimitiveType type;
};

class Model : public SceneObject
{
    public:
        std::vector<Primitive> primitives = {};
};

class Scene {
    public:
        std::vector<std::shared_ptr<Model>> models = {};
};
