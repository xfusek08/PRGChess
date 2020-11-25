#pragma once

#include <scene/Transform.h>

#include <string>

class Model
{
    public:
        Transform   transform     = Transform();
        std::string geometryIdent = "";
        std::string materialIdent = "";
};
