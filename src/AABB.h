
#pragma once

#include <scene/Scene.h>

struct BoundingBox {
    glm::vec3 min = glm::vec3(0);
    glm::vec3 max = glm::vec3(0);

    BoundingBox(glm::vec3 min, glm::vec3 max) : min(min), max(max) {}
};

class AABBHierarchy
{
    public:
        AABBHierarchy(const Scene& scene);
        void rebuild();

        BoundingBox geometryBB(const std::string& geometryId);

        glm::vec3 transformDimensions(glm::vec3 dimensions, Transform transform);

    private:
        const Scene& scene;
};
