## Prototipo de Raycasting (Unity)

Pequeño prototipo en Unity para pruebas de raycasting con un arma de ciencia ficción dentro de una escena industrial. Incluye controles básicos de movimiento, cámara y disparo, más efectos visuales y de audio.

## Requisitos

- Unity (versión LTS recomendada; abre el proyecto con tu versión instalada)
- Sistema operativo: Windows 10 o superior (desarrollado en win32 10.0.19045)

## Cómo abrir el proyecto

1. Abre Unity Hub y añade la carpeta del proyecto.
2. Abre la escena principal en `Scenes/SciFi_Industrial.unity`.
3. Pulsa Play para ejecutar.

## Controles por defecto

- Movimiento: WASD
- Mirar: Ratón
- Saltar: Barra espaciadora (si está habilitado en `PlayerMovement.cs`)
- Disparar: Clic izquierdo (lógica en `Gun.cs`)

Nota: Puedes ajustar sensibilidad, velocidades y otras opciones en los scripts de `Scripts/`.

## Estructura del proyecto (carpetas relevantes)

- `Scenes/`: contiene la escena `SciFi_Industrial.unity` y datos de iluminación/reflexión.
- `Scripts/`:
  - `Gun.cs`: lógica de disparo con raycasting y audio de disparo.
  - `MouseLook.cs`: control de cámara basado en ratón.
  - `PlayerMovement.cs`: movimiento del jugador.
- `Prefabs/Weapons/`: prefabs de armas de ciencia ficción.
- `Materials/`: materiales utilitarios del proyecto.
- `Audio/shotgun.mp3`: audio de disparo.
- `JMO Assets/WarFX/`: efectos visuales (partículas y shaders móviles).
- `AssetStoreOriginals/SciFi_Industrial/`: prefabs y materiales del entorno industrial.
- `AssetStore/WM_SciFi_Weapon1_Lite/`: modelos, materiales y texturas de arma sci‑fi.

## Configuración rápida

1. Arrastra un prefab de arma desde `Prefabs/Weapons/` a tu jerarquía si tu escena no lo tiene.
2. Asegúrate de que el jugador (o la cámara) tiene `MouseLook.cs` y `PlayerMovement.cs` según corresponda.
3. Revisa en `Gun.cs` las referencias (AudioSource, efectos, capas de colisión) si se usan en la escena.

## Construcción (Build)

1. Abre `File > Build Settings…`.
2. Añade `Scenes/SciFi_Industrial.unity` a `Scenes In Build`.
3. Selecciona la plataforma objetivo (p. ej., PC, Mac & Linux Standalone > Windows) y pulsa `Build`.

## Personalización y puntos de extensión

- `Gun.cs`: ajusta distancia del raycast, daño, capas a impactar, y efectos de impacto.
- `MouseLook.cs`: ajusta sensibilidad, límites de inclinación, bloqueo del cursor.
- `PlayerMovement.cs`: velocidades, salto, gravedad y suavizados.
- VFX: explora `JMO Assets/WarFX` para otros sistemas de partículas.
- Armas: sustituye prefabs en `Prefabs/Weapons/` o crea nuevos a partir de `WM_SciFi_Weapon1_Lite`.

## Dependencias y créditos de assets

Este proyecto incluye assets de terceros para fines de prototipo:

- `JMO Assets/WarFX` (VFX y utilidades de partículas)
- `AssetStoreOriginals/SciFi_Industrial` (escenario y props)
- `AssetStore/WM_SciFi_Weapon1_Lite` (arma sci‑fi, materiales y texturas)

Revisa las licencias de cada paquete en sus respectivas fuentes antes de distribuir.



