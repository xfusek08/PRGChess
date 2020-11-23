#pragma once

#include <scene/ModelGeometry.h>
#include <scene/Model.h>
#include <scene/Material.h>

#include <vector>
#include <string>
#include <map>

class Scene {
    public:
        std::map<std::string, ModelGeometry> geometries = {};
        std::map<std::string, Material>      materials  = {};
        std::vector<Model>                   models     = {};
};
