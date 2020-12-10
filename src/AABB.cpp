
#include <AABB.h>

using namespace std;

AABBHierarchy::AABBHierarchy(const Scene& scene) : scene(scene) {
    rebuild();
}

void AABBHierarchy::rebuild() {

}

BoundingBox AABBHierarchy::geometryBB(const std::string& geometryId) {
    BoundingBox bb = {};
    const auto& geometry = scene.geometries.at(geometryId);
    for (const auto& primitive : geometry.primitives) {
        if (primitive->operation == PrimitiveOperation::Add) {
            bb = bb.add(bbForPrimitive(*primitive));
        }
    }
    return bb;
}

BoundingBox AABBHierarchy::bbForPrimitive(const Primitive& primitive) {
    BoundingBox bbRes = {};
    glm::vec3 halfDimensions = primitive.getDimensions() / 2.0f;
    bbRes.max = { halfDimensions.x, halfDimensions.y, halfDimensions.z };
    bbRes.min = { -halfDimensions.x, -halfDimensions.y, -halfDimensions.z };
    return bbRes.transform(primitive.transform);
}

BoundingBox BoundingBox::transform(Transform tranform) {
    glm::vec4 corners[] = {
        // right side
        { max.x, max.y, max.z, 1.0f },
        { max.x, max.y, min.z, 1.0f },
        { max.x, min.y, max.z, 1.0f },
        { max.x, min.y, min.z, 1.0f },

        // left side
        { min.x, max.y, max.z, 1.0f },
        { min.x, max.y, min.z, 1.0f },
        { min.x, min.y, max.z, 1.0f },
        { min.x, min.y, min.z, 1.0f },
    };

    auto transformMatrix = glm::inverse(tranform.getTransform());

    BoundingBox bbRes = {};
    for (int i =0; i < 8; ++i) {
        auto transformed = transformMatrix * corners[i];
        bbRes.max = glm::max(bbRes.max, glm::vec3(transformed));
        bbRes.min = glm::min(bbRes.min, glm::vec3(transformed));
    }
    return bbRes;
}
