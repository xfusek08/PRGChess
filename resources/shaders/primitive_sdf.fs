#version 460 core

#define MAX_STEPS    100
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01

#define TYPE_SPHERE  0
#define TYPE_CAPSULE 1
#define TYPE_TORUS   2

struct Primitive {
    int type;
    vec4 position;
    vec4 rotationQuaternion;
    float data[4];
};

// here scene uniform buffer will be loaded
Primitive scene[] = {
    { TYPE_SPHERE,  vec4(0, 2,  0, 1), vec4(0), { 1.0, 0.0, 0.0, 0.0 } },
    { TYPE_CAPSULE, vec4(5, 4, -1, 1), vec4(0), { 0.5, 0.3, 0.0, 0.0 } },
    { TYPE_TORUS,   vec4(2, 2,  0, 1), vec4(0), { 4.0, 0.4, 0.0, 0.0 } },
};

int sceneSize = 3;

// SDF definitions
float sdSphere(vec3 position, Primitive sphere);
float sdCapsule(vec3 position, Primitive capsule);
float sdTorus(vec3 position, Primitive torus);

// MAIN portal to different primitives
float sdToScene(vec3 position) {
    float minDist = MAX_DISTANCE;
    for (int i = 0; i < sceneSize; ++i) {
        float actDist = 0;
        switch (scene[i].type) {
            case TYPE_SPHERE:  actDist = sdSphere(position, scene[i]);  break;
            case TYPE_CAPSULE: actDist = sdCapsule(position, scene[i]); break;
            case TYPE_TORUS: actDist   = sdTorus(position, scene[i]);   break;
        }
        minDist = min(actDist, minDist);
    }
    return min(minDist, position.y);
    // return minDist;
}

float sdSphere(vec3 position, Primitive sphere) {
    return length(position - sphere.position.xyz) - sphere.data[0];
}

float sdCapsule(vec3 position, Primitive capsule) {
    vec3 a = capsule.position.xyz + vec3(0,  1, 0) * capsule.data[0];
    vec3 b = capsule.position.xyz + vec3(0, -1, 0) * capsule.data[0];
    vec3 ab = b - a;
    vec3 ap = position - a;
    float t = clamp(dot(ab, ap) / dot(ab, ab), 0, 1);
    vec3 c = a + ab * t;
    return length(position - c) - capsule.data[1];
}

float sdTorus(vec3 position, Primitive torus) {
    vec3 p = position - torus.position.xyz;
    float x = length(p.xz) - torus.data[0];
    return length(vec2(x, p.y)) - torus.data[1];
}
