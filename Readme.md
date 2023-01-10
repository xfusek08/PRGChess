# PRGChess

This repo contains a finished project to "Computer graphics" course at

[Brno University
Of Technology](https://www.vut.cz/en) - [*Faculty Of InformationTechnology*](https://www.fit.vut.cz/.en).

This project is a simple scene renderer of a chessboard scene which is composed of Constructive solid geometry models of SDF primitives.

Scene is stored and loaded from `resources/scene.json` file.

Rendering is using a screen quad and a raymarching algorithm per-pixel in fragment shader.
## Installation and Running

### Prerequisites

- CMake 3.18 or newer
- C++ compiler with C++17 support
- OpenGL 4.3 or newer

### Cloning repo
*Repository contains all dependencies as submodules.*

```bash
git clone https://github.com/xfusek08/PRGChess.git --recursive
```

### Debug build

```bash
mkdir build && cd build
cmake -DCMAKE_BUILD_TYPE=Debug ..
make -j
```

### Release build

```bash
mkdir build && cd build
cmake -DCMAKE_BUILD_TYPE=Release ..
make -j
```

### Run the app

```bash
./PRGChess
```

**NOTE:** For binary distribution make sure that it is compiled in `Release` mode and that `resources` folder is in the same directory as the binary.

## Controls
Rotating with mouse while holding left mouse button.

## Screenshots
![screenshot1](screenshots/screenshot1.png)
![screenshot2](screenshots/screenshot2.png)
![screenshot3](screenshots/screenshot3.png)


## Authors
Petr Fusek

## Thank to and credits

- [glm](https://github.com/g-truc/glm)
- [json](https://github.com/nlohmann/json)
- [glad](https://glad.dav1d.de)
- [sdl](https://www.libsdl.org/)
