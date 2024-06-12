# FluidSimulation3D

A Eulerian 3D fluid simulation of smoke implemented in MonoGame utilizing the [Compute Fork by cpt-max](https://github.com/cpt-max/MonoGame). The smoke dynamics are calculated via a series of compute shaders and the final volume is rendered through a ray marcher. This was created as a final project in my CPI411 Graphics for Games class, feel free to read the [Lab Exercise](https://github.com/lukephilipps/FluidSimulation3D/blob/main/Fluid%20Simulation%20in%203D.pdf) I wrote coinciding with the project.

![Image of the simulation with the smoke absorption color set to a cyan blue.]([http://url/to/img.png](https://github.com/lukephilipps/FluidSimulation3D/blob/main/DemoScreenshot.png))

## Future Additions
In the future I want to improve the efficiency of this simulation while expanding the range of fluids it can simulate. I have some work on ComputeBuffer->RenderTexture in a fork, however I ran into some issues with MonoGame and surface formats seemingly only being UINTs. 
* Conversion of ComputeBuffers to RenderTexture3Ds for faster access patterns.
* Water simulation using the Level-set Method.
* Viscous fluid simulation.

## References
* [Scrawk's Unity Fluid Simulation](https://github.com/Scrawk/GPU-GEMS-3D-Fluid-Simulation?tab=readme-ov-file)
* [Keenan Crane's Box of Smoke](https://www.cs.cmu.edu/~kmcrane/Projects/GPUFluid/)
* [Shahriar Shahrabi's Gentle Introduction to Realtime Fluid Simulation for Programmers and Technical Artists](https://shahriyarshahrabi.medium.com/gentle-introduction-to-fluid-simulation-for-programmers-and-technical-artists-7c0045c40bac)
* [GPU Gems 3 Chapter 30 Real-Time Simulation and Rendering of 3D Fluids](https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-30-real-time-simulation-and-rendering-3d-fluids)
* [GPU Gems 1 Chapter 38 Fast Fluid Dynamics Simulation on the GPU](https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-38-fast-fluid-dynamics-simulation-gpu)
