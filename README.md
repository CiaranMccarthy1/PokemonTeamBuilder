# Pokémon Team Builder

A cross-platform .NET MAUI application to build and manage teams. 

## Features

### Pokédex Browser
- Browse all Pokémon with search and filtering options
- View detailed stats, types, abilities, and sprites
- Filter by type, generation, and other criteria
- Sort by various stats (HP, Attack, Defense, Speed, etc.)
- View regular and shiny sprite variants

### Team Builder
- Create and manage multiple Pokémon teams
- Each team supports up to 6 Pokémon
- Custom team naming

### Type Coverage Analyzer
- Analyze team strengths and weaknesses
- View offensive and defensive type coverage
- Identify coverage gaps in your team

### Stat Comparison
- Compare 2-4 Pokémon side-by-side
- Visual bar charts for all base stats
- Compare HP, Attack, Defense, Sp. Attack, Sp. Defense, and Speed

### Settings
- Dark/Light theme toggle
- Redownload Pokémon data

### Data Persistence
- Teams saved locally in JSON format
- Pokémon data cached for offline viewing
- Favorites list

## Requirements

- .NET 9.0 SDK
- Visual Studio 2022 (17.8 or later) with .NET MAUI workload
- Windows 10/11 or Android device/emulator for testing


### First Run

On first launch, the app will download and cache Pokémon data from the PokéAPI. This ensures fast performance and offline capability for subsequent uses.

### Endpoints Used
- `pokeapi.co/api/v2/pokemon` - Pokémon list and details
- `pokeapi.co/api/v2/type` - Type information
- `pokeapi.co/api/v2/ability` - Ability details
- `pokeapi.co/api/v2/evolution-chain` - Evolution chains
