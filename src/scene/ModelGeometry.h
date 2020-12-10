#pragma once

#include <scene/Primitive.h>

#include <vector>

class ModelGeometry
{
    public:
        std::vector<std::shared_ptr<Primitive>> primitives = {};
        ModelGeometry(std::vector<std::shared_ptr<Primitive>> primitives) : primitives(primitives) {}
        template <typename... ARGS>
        ModelGeometry(ARGS... primitives) : primitives(std::vector<std::shared_ptr<Primitive>>({primitives...})){}
};
