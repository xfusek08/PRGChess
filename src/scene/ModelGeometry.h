#pragma once

#include <scene/Primitive.h>

#include <vector>

class ModelGeometry
{
    public:
        std::vector<Primitive> primitives = {};

        ModelGeometry(std::vector<Primitive> primitives) : primitives(primitives) {}

        template <typename... ARGS>
        ModelGeometry(ARGS... primitives) : primitives(std::vector<Primitive>({primitives...})){}
};
