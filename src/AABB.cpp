
#include <AABB.h>
#include <map>
#include <algorithm>

#ifdef DEBUG
#include <RenderBase/tools/logging.h>
#endif

using namespace std;

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

BoundingBox BoundingBox::transform(const Transform& tranform) const {
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


AABBHierarchy::AABBHierarchy(const Scene& scene) : scene(scene) {
    rebuild();
}

using AABBNodeList = vector<std::shared_ptr<AABBNode>>;

AABBNodeList mergeAABBNodeListInHalf(AABBNodeList nodes) {

    AABBNodeList result;
    result.reserve(glm::ceil(nodes.size() / 2));
    vector<bool> used(nodes.size(), false);

    // compute pair distance
    multimap<float, tuple<int, int>> distancePairs;
    for (int actIndex = 0; actIndex < nodes.size(); ++actIndex) {
        auto actNode = nodes[actIndex];
        if (actIndex + 1 == nodes.size()) {
            if (nodes.size() % 2 == 1) { // send the odd one to result directly
                used[actIndex] = true;
                result.push_back(actNode);
            }
        } else {
            for (int otherIndex = actIndex + 1; otherIndex < nodes.size(); ++otherIndex) {
                auto other = nodes[otherIndex];
                auto dist = actNode->distance(*other);
                distancePairs.emplace(dist, make_tuple(actIndex, otherIndex));
            }
        }
    }

    // get the closest pairs and create result list
    for (const auto& pair : distancePairs) {
        auto firstIndex = get<0>(pair.second);
        auto secondIndex = get<1>(pair.second);
        if (!used[firstIndex] && !used[secondIndex]) {
            auto firstNode = nodes[firstIndex];
            auto secondNode = nodes[secondIndex];
            auto newNode = make_shared<AABBNode>();
            newNode->box = firstNode->box.add(secondNode->box);
            newNode->left = firstNode;
            newNode->right = secondNode;
            result.push_back(newNode);
            used[firstIndex] = true;
            used[secondIndex] = true;

            // check if all indices were used.
            if (all_of(used.begin(), used.end(), [](bool v) { return v; })) {
                break;
            }
        }
    }

    return move(result);
}

void AABBHierarchy::rebuild() {

    AABBNodeList nodes;

    int id = 0;
    for (const auto& model : scene.models) {
        auto newNode = make_shared<AABBNode>();
        newNode->box = geometryBB(model.geometryIdent).transform(model.transform);
        newNode->modelId = id;
        nodes.push_back(newNode);
        ++id;
    }
    while (nodes.size() > 1) {
        nodes = mergeAABBNodeListInHalf(nodes);
    }

    root = nodes.front();
}

#ifdef DEBUG

void AABBNode::debugPrint(int level) {
    auto prefix = string(level * 2, ' ');

    if (left != nullptr) {
        LOG_DEBUG(prefix << "Child 1:");
        left->debugPrint(level + 1);
    }
    if (right != nullptr) {
        LOG_DEBUG(prefix << "Child 2:");
        right->debugPrint(level + 1);
    }
    LOG_DEBUG(prefix << "BBCenter: " << glm::to_string(box.center()));
}

#endif