
#pragma once

#include <scene/Scene.h>

struct BoundingBox {
    glm::vec3 min = glm::vec3(FLT_MAX);
    glm::vec3 max = glm::vec3(-FLT_MAX);

    BoundingBox() {}
    BoundingBox(glm::vec3 min, glm::vec3 max) : min(min), max(max) {}

    BoundingBox add(BoundingBox otherBb) {
        return {
            glm::min(min, otherBb.min),
            glm::max(max, otherBb.max)
        };
    }

    BoundingBox transform(Transform transform);
};

class AABBHierarchy
{
    public:
        AABBHierarchy(const Scene& scene);
        void rebuild();

        BoundingBox geometryBB(const std::string& geometryId);

        // glm::vec3 transformDimensions(glm::vec3 dimensions, Transform transform);

        // BoundingBox applyRotation(BoundingBox bb, Transform transform);

        BoundingBox bbForPrimitive(const Primitive& primitive);

    private:
        const Scene& scene;
};
