
#pragma once

#include <scene/Scene.h>

struct BoundingBox {
    glm::vec3 min = glm::vec3(FLT_MAX);
    glm::vec3 max = glm::vec3(-FLT_MAX);

    BoundingBox() {}
    BoundingBox(glm::vec3 min, glm::vec3 max) : min(min), max(max) {}

    inline glm::vec3 center()                          const { return min + (max-min) / 2.0f; }
    inline BoundingBox add(const BoundingBox& otherBb) const { return { glm::min(min, otherBb.min), glm::max(max, otherBb.max) }; }
    inline float distance(const BoundingBox& otherBb)  const { return glm::distance(center(), otherBb.center()); }

    BoundingBox transform(const Transform& transform)  const;
};


class AABBNode
{
    public:
        std::shared_ptr<AABBNode> left = nullptr;
        std::shared_ptr<AABBNode> right = nullptr;
        BoundingBox box = {};
        int modelId = -1;

        inline float distance(const AABBNode& other) const { return box.distance(other.box); }

        #ifdef DEBUG
        void debugPrint(int level = 0);
        #endif
};

class AABBHierarchy
{
    public:
        std::shared_ptr<AABBNode> root;

        AABBHierarchy(const Scene& scene);
        void rebuild();

        BoundingBox geometryBB(const std::string& geometryId);
        BoundingBox bbForPrimitive(const Primitive& primitive);
    private:
        const Scene& scene;
};
