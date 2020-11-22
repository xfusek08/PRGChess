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

uint raySteps = 0;

float rayMarch(vec3 originPoint, vec3 direction) {
    float distanceMarched = 0;
    for (int step = 0; step < MAX_STEPS; ++step) {
        ++raySteps;
        vec3 position = originPoint + distanceMarched * direction;
        distanceMarched += sdToScene(position);
        if (distanceMarched > MAX_DISTANCE || distanceMarched < HIT_DISTANCE) {
            break; // hit
        }
    }
    return distanceMarched;
}

vec3 getLight(vec3 point) {
    vec3 toLightVector    = normalize(lightPosition - point);
    vec3 viewVector       = normalize(cameraPosition - point);
    vec3 normalVector     = getNormal(point);
    vec3 reflectionVector = normalize(reflect(-toLightVector, normalVector));

    float dotNL = max(dot(normalVector, toLightVector), 0.0);
    float dotRV = max(dot(reflectionVector, viewVector), 0.0);

    // material properties
    vec3  materialcolor = vec3(0.7, 0.2, 0.2);
    vec3  specularColor = vec3(1.0, 1.0, 1.0);
    float shininess     = 10;

    // light properties
    vec3 lightIntensity = vec3(0.5, 0.5, 0.5);
    vec3 ambientLight   = materialcolor  * vec3(0.3, 0.3, 0.6);
    vec3 diffuseLight   = materialcolor  * dotNL;
    vec3 specularLight  = specularColor * pow(dotRV, shininess);

    return lightIntensity * (ambientLight + diffuseLight + specularLight);
}

void main(){
    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    float dist         = rayMarch(cameraPosition, rayDirection);
    vec3  color        = vec3(0.2, 0.2, 0.3);

    if (dist < MAX_DISTANCE) {
        vec3 position = cameraPosition + rayDirection * dist;
        color         = vec3(getLight(position));
    }

    // color = vec3(
    //     clamp(raySteps / float(4*MAX_STEPS), 0.0, 0.9)
    //     // , clamp((raySteps-MAX_STEPS) / float(2*MAX_STEPS), 0.0, 0.9)
    //     // , 0
    //     // , clamp((raySteps - 2*MAX_STEPS) / float(2*MAX_STEPS), 0.0, 0.9)
    // );

    fColor            = vec4(color, 1);
}
