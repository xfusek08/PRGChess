#pragma once

#include <glm/glm.hpp>

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
    glm::mat4 getRotationMatrix() const;

    // a syntax sugar
    inline void scale(float size)                    { this->size     += size; }
    inline void translate(glm::vec3 position)        { this->position += position; }
    inline void rotate(glm::vec3 rotation)           { this->rotation += rotation; }

    inline void translate(float x, float y, float z) { translate({x, y, z}); }
    inline void rotate(float x, float y, float z)    { rotate({x, y, z}); }
};
