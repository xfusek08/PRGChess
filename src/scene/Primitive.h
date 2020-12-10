#pragma once

#include <scene/Transform.h>
#include <unordered_map>
#include <memory>

#define GET_SET_F(name, targetProperty, getFormula, setFormula) \
    inline float get##name() const { float value = get##targetProperty(); return (getFormula); } \
    inline void set##name(float value) { set##targetProperty(setFormula); }
#define GET_SET_DOUBLE(name, targetProperty) GET_SET_F(name, targetProperty, value * 2.0f, value / 2.0)
#define GET_SET(name, dataTarget) \
    inline float get##name() const { return dataTarget; } \
    inline void set##name(float value) { dataTarget = (value); }

enum PrimitiveType {
    ptSphere    = 0,
    ptCapsule   = 1,
    ptTorus     = 2,
    ptBox       = 3,
    ptCilinder  = 4,
    ptCone      = 5,
    ptRoundCone = 6,
    ptInvalid = 100,
};

enum PrimitiveOperation {
    Add       = 0,
    Substract = 1,
    Intersect = 2,
    poInvalid = 100,
};

static const std::unordered_map<std::string, PrimitiveType> primitiveTypeDict = {
    {"sphere",    PrimitiveType::ptSphere},
    {"capsule",   PrimitiveType::ptCapsule},
    {"torus",     PrimitiveType::ptTorus},
    {"box",       PrimitiveType::ptBox},
    {"cilinder",  PrimitiveType::ptCilinder},
    {"ciln",      PrimitiveType::ptCilinder},
    {"cone",      PrimitiveType::ptCone},
    {"roundcone", PrimitiveType::ptRoundCone},
    {"rcone",     PrimitiveType::ptRoundCone},
};


static const std::unordered_map<std::string, PrimitiveOperation> primitiveoperationDict = {
    {"add",       PrimitiveOperation::Add},
    {"substract", PrimitiveOperation::Substract},
    {"sub",       PrimitiveOperation::Substract},
    {"intersect", PrimitiveOperation::Intersect},
    {"isect",     PrimitiveOperation::Intersect},
};

struct Primitive
{
    Transform          transform = Transform();
    PrimitiveOperation operation = PrimitiveOperation::Add;
    glm::vec4          data      = glm::vec4(0.0f);
    float              blending  = 0.0f;

    virtual PrimitiveType getType()       const;
    virtual glm::vec3     getDimensions() const;

    virtual void setDataPropertyByName(const std::string& name, float value) {}

    static PrimitiveType              typeFromString(const std::string& name);
    static PrimitiveOperation         operationFromString(const std::string& name);
    static std::shared_ptr<Primitive> createByType(PrimitiveType type);
};

struct Sphere : Primitive {
    GET_SET(Radius, data.x)
    GET_SET_DOUBLE(Diameter, Radius)

    Sphere() : Primitive() {
        setRadius(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct Capsule : Primitive {
    GET_SET(HalfWidth,  data.x)
    GET_SET(HalfHeight, data.y)
    GET_SET(Radius,     data.z)

    GET_SET_DOUBLE(Width,    HalfWidth)
    GET_SET_DOUBLE(Height,   HalfHeight)
    GET_SET_DOUBLE(Diameter, Radius)

    Capsule() : Primitive() {
        setHalfWidth(0.5);
        setHalfHeight(0.5);
        setRadius(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct Torus : Primitive {
    GET_SET(RadiusInner,  data.x)
    GET_SET(RadiusOuther, data.y)

    GET_SET_DOUBLE(DiameterInner, RadiusInner)
    GET_SET_DOUBLE(DiameterOuther, RadiusOuther)

    Torus() : Primitive() {
        setRadiusInner(0.5);
        setRadiusOuther(0.25);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct Box : Primitive {
    GET_SET(HalfWidth,  data.x)
    GET_SET(HalfHeight, data.y)
    GET_SET(HalfDepth,  data.z)
    GET_SET(Rounding,   data.w)

    GET_SET_DOUBLE(Width,  HalfWidth)
    GET_SET_DOUBLE(Height, HalfHeight)
    GET_SET_DOUBLE(Depth,  HalfDepth)

    Box() : Primitive() {
        setHalfWidth(0.5);
        setHalfHeight(0.5);
        setHalfDepth(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct Cilinder : Primitive {
    GET_SET(Radius,     data.x)
    GET_SET(HalfHeight, data.y)
    GET_SET(Rounding,    data.z)

    GET_SET_DOUBLE(Width,    Radius)
    GET_SET_DOUBLE(Diameter, Radius)
    GET_SET_DOUBLE(Height,   HalfHeight)

    Cilinder() : Primitive() {
        setRadius(0.5);
        setHalfHeight(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct Cone : Primitive {
    GET_SET(RadiusTop,    data.y)
    GET_SET(RadiusBottom, data.x)
    GET_SET(HalfHeight,   data.z)
    GET_SET(Rounding,     data.w)

    GET_SET_DOUBLE(DiameterTop,    RadiusTop)
    GET_SET_DOUBLE(DiameterBottom, RadiusBottom)
    GET_SET_DOUBLE(Height,         HalfHeight)

    Cone() : Primitive() {
        setRadiusTop(0.25);
        setRadiusBottom(0.5);
        setHalfHeight(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};

struct RoundCone : Primitive {
    GET_SET(RadiusTop,    data.y)
    GET_SET(RadiusBottom, data.x)
    GET_SET(HalfHeight,   data.z)

    GET_SET_DOUBLE(DiameterTop,    RadiusTop)
    GET_SET_DOUBLE(DiameterBottom, RadiusBottom)
    GET_SET_DOUBLE(Height,         HalfHeight)

    RoundCone() : Primitive() {
        setRadiusTop(0.25);
        setRadiusBottom(0.5);
        setHalfHeight(0.5);
    }

    PrimitiveType getType()   const override;
    glm::vec3 getDimensions() const override;

    void setDataPropertyByName(const std::string& name, float value) override;
};
