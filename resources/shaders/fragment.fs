#version 460 core

#define MAX_STEPS    100
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01

in vec2 fragCoord;
in vec3 rayDirection;
out vec4 fColor;

float sdToScene(vec3 position);

// uniforms

// camera
uniform vec3 cameraPosition;
uniform vec3 cameraDirection;
uniform vec3 upRayDistorsion;
uniform vec3 leftRayDistorsion;

uniform vec3  lightPosition;
uniform vec3  ambientLight;

vec3 getNormal(vec3 point) {
    float d = sdToScene(point);
    vec2 e = vec2(HIT_DISTANCE, 0);
    vec3 n = d - vec3(
        sdToScene(point - e.xyy),
        sdToScene(point - e.yxy),
        sdToScene(point - e.yyx)
    );

    return normalize(n);
}

float rayMarch(vec3 originPoint, vec3 direction) {
    float distanceMarched = 0;
    for (int step = 0; step < MAX_STEPS; ++step) {
        vec3 position = originPoint + distanceMarched * direction;
        distanceMarched += sdToScene(position);
        if (distanceMarched > MAX_DISTANCE || distanceMarched < HIT_DISTANCE) {
            break; // hit
        }
    }
    return distanceMarched;
}

vec3 getLight(vec3 point) {
    vec3 l = normalize(lightPosition - point);
    vec3 n = getNormal(point);
    float diff = clamp(dot(n, l), 0.0, 1.0);
    float distToLight = rayMarch(point + n * HIT_DISTANCE * 2, l);

    if (distToLight < length(lightPosition - point)) {
        return ambientLight;
    }

    return clamp(ambientLight + vec3(diff), 0, 1);
}

void main(){
    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    float dist         = rayMarch(cameraPosition, rayDirection);
    vec3  color        = vec3(0.2, 0.2, 0.3);

    if (dist < MAX_DISTANCE) {
        vec3 position = cameraPosition + rayDirection * dist;
        color         = vec3(getLight(position));
    }

    fColor            = vec4(color, 1);
}
