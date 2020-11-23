
#include <AABB.h>

using namespace std;

glm::vec3 minPerComp(glm::vec3 v1, glm::vec3 v2) {
    return {
        glm::min(v1.x, v2.x),
        glm::min(v1.y, v2.y),
        glm::min(v1.z, v2.z),
    };
}

glm::vec3 maxPerComp(glm::vec3 v1, glm::vec3 v2) {
    return {
        glm::max(v1.x, v2.x),
        glm::max(v1.y, v2.y),
        glm::max(v1.z, v2.z),
    };
}

AABBHierarchy::AABBHierarchy(const Scene& scene) : scene(scene) {
    rebuild();
}

void AABBHierarchy::rebuild() {

}

BoundingBox AABBHierarchy::geometryBB(const std::string& geometryId) {
    const auto& geometry = scene.geometries.at(geometryId);

    glm::vec3 maxPositive = glm::vec3(-FLT_MAX);
    glm::vec3 minNegative = glm::vec3(FLT_MAX);

    for (const auto& primitive : geometry.primitives) {
        if (primitive.operation == PrimitiveOperation::Add) {
            auto dimensions = transformDimensions(primitive.getDimensions(), primitive.transform);
            maxPositive = maxPerComp(maxPositive, dimensions + primitive.transform.position);
            minNegative = minPerComp(minNegative, -dimensions + primitive.transform.position);
        }
    }

    return BoundingBox(minNegative, maxPositive);
}

glm::vec3 AABBHierarchy::transformDimensions(glm::vec3 dimensions, Transform transform) {
    dimensions *= 0.5f; // working with dimesions from center to edge

    glm::vec4 corners[] = {
        // right side
        { + dimensions.x, + dimensions.y, + dimensions.z, 1.0f },
        { + dimensions.x, + dimensions.y, - dimensions.z, 1.0f },
        { + dimensions.x, - dimensions.y, + dimensions.z, 1.0f },
        { + dimensions.x, - dimensions.y, - dimensions.z, 1.0f },

        // left side
        { - dimensions.x, + dimensions.y, + dimensions.z, 1.0f },
        { - dimensions.x, + dimensions.y, - dimensions.z, 1.0f },
        { - dimensions.x, - dimensions.y, + dimensions.z, 1.0f },
        { - dimensions.x, - dimensions.y, - dimensions.z, 1.0f },
    };

    auto transformMatrix = transform.getRotationMatrix();

    glm::vec3 maxPositive = glm::vec3(-FLT_MAX);
    glm::vec3 minNegative = glm::vec3(FLT_MAX);

    for (int i =0; i < 8; ++i) {
        auto transformed = transformMatrix * corners[i];
        maxPositive = maxPerComp(maxPositive, transformed);
        minNegative = minPerComp(minNegative, transformed);
    }

    return (maxPositive - minNegative) * 0.5f;
}