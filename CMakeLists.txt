cmake_minimum_required (VERSION 3.18)
project (PRGChess)

set(CMAKE_CXX_STANDARD 17)

# find shaders
set(SHADER_FILES
    vertex.vs
    fragment.fs
)
include(vendor/RenderBase/cmakeUtils/LoadSharedFiles.cmake)

set(SOURCES
    src/main.cpp
)

set(COMPILE_DEFINITIONS
    "PROJECT_NAME=\"${PROJECT_NAME}\""
    $<$<CONFIG:Debug>:
        "DEBUG"
        ${SHADER_DEBUG_COMPILE_DEFINITIONS}
    >
    $<$<CONFIG:Release>:
        ${SHADER_RELEASE_COMPILE_DEFINITIONS}
    >
)

add_subdirectory(vendor/RenderBase)

add_executable(${PROJECT_NAME}
    ${SOURCES}
)

target_link_libraries(${PROJECT_NAME} RenderBase)

target_include_directories(${PROJECT_NAME} PUBLIC
    src
)

message(" ")
message("====== ${PROJECT_NAME} compile definitions =======")
message(${COMPILE_DEFINITIONS})
message("==================================================")
message(" ")
target_compile_definitions(${PROJECT_NAME} PUBLIC ${COMPILE_DEFINITIONS})
