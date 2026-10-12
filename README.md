# Overtime Express

A mobile parkour runner where the clock is your life. You are Kip, a courier who has just been handed his own
delivery company: one member of staff (you), no vehicles, and a city of rooftops between you and every customer.

Play the current build: https://overtime-hazel-three.vercel.app

## How it plays

- **The clock is everything.** Seconds are your health, your fuel and your pay. Chaining moves earns time, crashing costs it.
- **Overtime.** At zero you don't stop: the city turns red, you speed up, points double, and one hit ends the run.
- **Every parcel changes the rules.** Heavy parcels smash through hurdles, fragile ones crack on hard landings, hot ones speed up the clock.
- **Power-ups from a courier's day.** Burger rush, soda fizz, bubble wrap and the Tock magnet.
- **Tocks.** The city's money is time: hex tokens with clock hands, picked up on the run and paid as wages.
- **Wages and gear.** Deliveries pay wages; wages buy gear; the last thing in the shop is the bike.

One thumb: swipe left and right to change lane (again to run a wall), swipe up or tap to jump, tap in the air to dash,
swipe down to slide, hold to slow time.

## Project layout

Unity 6 (6000.3 LTS), Universal Render Pipeline. Open the folder in Unity and press Play; the scene is built in code.

- `Assets/Overtime/Core` : the game rules as plain C# with no Unity dependencies (simulation, levels, a bot that proves every level is clearable)
- `Assets/Overtime/Game` : everything you see: world builder, character, camera, effects, interface
- `Assets/Overtime/Resources/Shaders` : sky, wet rooftops, towers and effects
- `Assets/Overtime/Editor` : asset baking, build and screenshot tools
- `ot.sh` : helper for driving the editor from the Unity CLI

## Credits

- Characters and animations: Quaternius (CC0), including the Universal Animation Library
- Props, coins and food: KayKit by Kay Lousberg (CC0)
- Fonts: Chakra Petch and Lilita One (SIL Open Font License)
