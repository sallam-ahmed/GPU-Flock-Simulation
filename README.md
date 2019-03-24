### GPU-Flock-Simulation
Untiy3D Implementation of flock simulation, GPU Powered.

**Implementation Details:**

Computation happens on the GPU using _ComputeShaders_, then the buffer with data is set in another _SurfaceShader_ that calculates the view matrix for each boid -flock handled as a big mesh-, they're then rendered on the screen, thus eliminating the overhead of recieving the data from the GPU buffer to the CPU and drawing them again.

**How to Run:**

Start the sample scene ***Scenes/BasicFlocking*** and hit play.

Tweak the parameters of ***BasicFlockController*** to control the general behaviour of boids.

**Unity Version:**

- 2018.3.6.9f1

**Inspired by:**

* [Shinao](https://github.com/Shinao).
* [Nature Of Code](https://natureofcode.com/).
* [Keijiro](https://github.com/keijiro).
