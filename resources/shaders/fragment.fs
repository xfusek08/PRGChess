#version 460 core

#define MAX_STEPS    100
#define MAX_DISTANCE 1000.0
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

vec3 getNormal(vec3 point) {
    float d = sdToScene(point);
    vec2 e = vec2(0.01, 0);
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

float getLight(vec3 point) {
    vec3 l = normalize(lightPosition - point);
    vec3 n = getNormal(point);

    float diff = clamp(dot(n, l), 0.0, 1.0);
    float distToLight = rayMarch(point + n * HIT_DISTANCE * 2, l);

    if (distToLight < length(lightPosition - point)) {
        return diff * 0.1;
    }
    return diff;
}

void main(){
    vec3 rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    float dist        = rayMarch(cameraPosition, rayDirection);
    vec3 position     = cameraPosition + rayDirection * dist;

    vec3 color;
    // axes
    if (dist < (MAX_DISTANCE / 2) - 1 && abs(position.x) < 0.05) {
        if (position.z > 0 ) {
            color = vec3(0.4, 0.7, 1.0); // z axis positive
        } else {
            color = vec3(0.1, 0.1, 1.0); // z axis negative
        }
    } else if (dist < (MAX_DISTANCE / 2) - 1 && abs(position.z) < 0.05) {
        if (position.x > 0 ) {
            color = vec3(1, 0.1, 0.1); // x axis positive
        } else {
            color = vec3(1, 0.7, 0.4); // x axis negative
        }
    } else {
        color = vec3(getLight(position));
    }
    fColor = vec4(color, 1);
}
