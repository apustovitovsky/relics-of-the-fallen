# Locomotion domain logic

`Docs/Locomotion` is the canonical source for character locomotion rules.

When adapting movement to multiplayer, preserve its gameplay behaviour:
walk, run, sprint, aim/lock-on strafing, jump, fall, crouch, sliding, turn-in-place, and movement-driven animation states.

Only the execution model changes:
the server performs authoritative simulation and moves `CharacterController`;
clients receive state and present animation, camera, VFX, and audio.