cmake_minimum_required (VERSION 3.18)
project (PRGChess)

set(CMAKE_CXX_STANDARD 17)

# load resource paths
set(RESOURCE_FILES
    # shaders
    shaders/vertex.vs
    shaders/fragment.fs
    shaders/primitive_sdf.fs

    # scene
    scene.json
)
include(vendor/RenderBase/cmakeUtils/LoadResourceFiles.cmake)

set(SOURCES
    src/main.cpp
    src/sceneUtils.h src/sceneUtils.cpp
    src/AABB.h src/AABB.cpp

    # scene
    src/scene/Transform.h src/scene/Transform.cpp
    src/scene/Primitive.h src/scene/Primitive.cpp
    src/scene/ModelGeometry.h
    src/scene/Material.h
    src/scene/Model.h
    src/scene/Scene.h

    # vendor - to lazy to write header only lib cmake
)

set(COMPILE_DEFINITIONS
    "PROJECT_NAME=\"${PROJECT_NAME}\""

    # vscode intelisence  cannot recognize the genrator pattern so it has to be hard-coded
    "DEBUG"
    ${RESOURCES_DEBUG_COMPILE_DEFINITIONS}

    # Proper compile redinitions
    # $<$<CONFIG:Debug>:
    #     "DEBUG"
    #     ${SHADER_DEBUG_COMPILE_DEFINITIONS}
    # >
    # $<$<CONFIG:Release>:
    #     ${SHADER_RELEASE_COMPILE_DEFINITIONS}
    # >
)

add_subdirectory(vendor/RenderBase)
add_subdirectory(vendor/json)

add_executable(${PROJECT_NAME}
    ${SOURCES}
)

target_link_libraries(${PROJECT_NAME} RenderBase json)

target_include_directories(${PROJECT_NAME} PUBLIC
    src
)

message(" ")
message("====== ${PROJECT_NAME} compile definitions =======")
message(${COMPILE_DEFINITIONS})
message("==================================================")
message(" ")
target_compile_definitions(${PROJECT_NAME} PUBLIC ${COMPILE_DEFINITIONS})
