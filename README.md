# Salmon Run

An educational 3D game that simulates the journey of Pacific BC salmon, aimed at raising awareness about the challenges these remarkable fish face throughout their lifecycle. Built in Unity, this game follows a sockeye salmon's journey from the ocean back to their spawning grounds.

## Overview

The game combines realistic (kind of) fish behavior simulation with gameplay mechanics to teach players about:

- The salmon lifecycle and migration patterns
- Environmental challenges facing Pacific salmon
- The impact of climate change on salmon populations
- The interconnected nature of ocean, river, and forest ecosystems

## Game Features

### Multi-Stage Journey

- **Ocean Phase**: Start your journey in the open ocean, avoiding fishing boats and predators while gathering strength
- **River Mouth**: Navigate past hungry harbor seals as you transition from saltwater to freshwater
- **Upper River**: Swim upstream against the current while evading bears and other land-based predators
- **Spawning Grounds**: Complete your lifecycle by reaching the final destination where salmon lay their eggs

### Realistic Fish Behavior

- **Schooling AI**: Control one fish while managing an intelligent school that follows realistic flocking behaviors
- **Boid System**: Advanced fish movement using cohesion, separation, and alignment principles
- **Dynamic Fish Selection**: When your current fish dies, seamlessly select and control another member of your school
- **Natural Movement**: AI fish exhibit independent movement patterns while maintaining formation

### Survival Mechanics

- **Health and Energy System**: Manage stamina and health while swimming long distances
- **Predator Avoidance**: Each predator type has unique behavior patterns and weaknesses
- **Environmental Adaptation**: Fish change appearance and behavior as they transition between environments
- **Struggle System**: Attempt to escape when caught by predators

### Educational Content

- **Lifecycle Education**: Learn about the complete salmon lifecycle through immersive gameplay
- **Climate Impact**: Understand how global warming affects salmon migration and survival
- **Conservation Awareness**: Includes information about the Pacific Salmon Foundation and conservation efforts

## Gameplay Mechanics

### Controls

- **SPACE**: Swim/Move forward, Struggle when caught
- **A/D**: Turn left/right
- **W/S**: Pitch up/down (in river environments)
- **SHIFT**: Sprint (consumes extra energy)

### Scoring System

- Ocean Phase: 1 point per surviving fish
- River Phases: 10 points per surviving fish
- Spawning Grounds: 20 points per surviving fish (double points)

### Fish Management

- Start with a school of fish and recruit more by collecting food
- When your controlled fish dies, enter selection mode to choose a new leader
- Use A/D to navigate between available fish and SPACE to confirm selection
- Ghost fish indicator helps visualize your selection

## Technical Features

### NPC AI Systems

- **Predator AI**: Multiple predator types with unique hunting behaviors
  - Seals: Must surface for air, hunt in water
  - Bears: Hunt from land, carry fish to feeding spots
  - Boats: Move in patterns, cause area damage
- **School Management**: Sophisticated fish school coordination system
- **Dynamic Level Transitions**: Seamless movement between ocean, river, and spawning environments

### Visual Systems

- **Water Rendering**: Custom water shader with underwater visibility
- **Fish Animation**: Detailed fish models with species-appropriate changes throughout the journey
- **Environmental Transitions**: Smooth transitions between different aquatic environments
- **Ghost Fish**: Translucent selection indicator for choosing new fish

### Performance Optimization

- **Object Pooling**: Efficient spawning and despawning of fish and environmental objects
- **Level-of-Detail**: Optimized rendering for large schools of fish
- **Spatial Partitioning**: Efficient neighbor detection for schooling behavior

## Educational Impact

The game incorporates real scientific information about salmon migration patterns, environmental challenges, and conservation efforts. Players learn about:

- The remarkable navigation abilities of salmon returning to their birthplace
- The physical transformation salmon undergo during their journey
- The role of salmon in supporting entire ecosystems
- Current threats to salmon populations including climate change, habitat destruction, and overfishing
- Conservation efforts and how individuals can help protect salmon populations

The game includes references to the Pacific Salmon Foundation and encourages players to learn more about real-world conservation efforts.

## Technical Requirements

### Unity Version

- Built with Unity 2022.3 LTS or compatible version

### Platform Support

- Windows (primary target)
- Built executable available in the Build folder

### Dependencies

- Standard Unity packages
- Custom fish behavior and schooling systems
- Advanced water rendering shaders

## Project Structure

The codebase is organized into several key systems:

- **Movement**: Player and AI fish movement controllers for different environments
- **AI**: Sophisticated predator and schooling behavior systems
- **World**: Game state management, level transitions, and scoring
- **Player**: Health, energy, and statistics management
- **UI**: User interface and educational content display
- **Spawner**: Dynamic object spawning for predators, food, and environmental elements

## Development

This game represents a significant technical achievement in simulating realistic fish behavior while maintaining engaging gameplay. The project demonstrates advanced Unity development techniques including:

- Complex AI behavior trees for multiple species
- Real-time flocking simulation for large groups
- Dynamic level generation and object management
- Sophisticated camera and player switching systems
- Performance-optimized rendering for underwater environments
