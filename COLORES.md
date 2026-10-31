# Colores de Quimikaze

| Elemento | Antes | Ahora |
|---|---|---|
| Fondo de las pantallas | Azul casi negro, plano | Degradado de violeta a azul profundo, con un patrón suave de anillos hexagonales (como moléculas) |
| Marcos | Gris oscuro con líneas de neón | Marco redondeado del color de la pantalla (cian, dorado, verde…) |
| Títulos | Texto de color suelto | Cinta de color con letras oscuras, como las etiquetas de un juego |
| Tarjetas y botones | Rectángulos grises y azules | Esquinas redondeadas, colores vivos y brillo arriba |
| Aviso de la tabla | Recuadro oscuro | Globo violeta con borde amarillo y un ícono "?" |

Contraste del texto blanco: 9.6:1 sobre el fondo y 6.8:1 sobre las tarjetas. El texto secundario queda en 5:1 o más. Todo cumple el mínimo de 4.5:1 de las pautas WCAG ([W3C](https://www.w3.org/TR/WCAG22/#contrast-minimum)).

## Por qué
- **Fondo oscuro, pero de color.** En visores VR, el texto claro sobre fondo oscuro cansa menos la vista y se lee mejor ([Erickson et al., 2020](https://doi.org/10.1109/VR46266.2020.00064)). Por eso el fondo sigue siendo oscuro. Pero a los niños los colores oscuros como el negro, el gris y el café les provocan más emociones negativas que los colores vivos ([Boyatzis y Varghese, 1994](https://doi.org/10.1080/00221325.1994.9914760)). Por eso el fondo es violeta-azul y no negro-gris.
- **Azul y violeta.** Están entre los tonos más agradables ([Valdez y Mehrabian, 1994](https://doi.org/10.1037/0096-3445.123.4.394)), y el azul se asocia con cosas que gustan, como el cielo y el agua limpia ([Palmer y Schloss, 2010](https://doi.org/10.1073/pnas.0906172107)). La saturación y el brillo influyen en la emoción más que el tono. Por eso los acentos son vivos y brillantes.
- **Brillo moderado.** En niños de 4 a 7 años, los colores más claros y no tan saturados atrajeron más la mirada ([Chen et al., 2023](https://doi.org/10.3389/fpsyg.2023.1205213)). Por eso hay colores pastel en las etiquetas y tonos claros en la parte alta de cada panel.
- **Formas redondeadas.** Las personas prefieren los objetos curvos a los de ángulos agudos ([Bar y Neta, 2006](https://doi.org/10.1111/j.1467-9280.2006.01759.x)).
- **Los colores de la tabla no cambian.** Son los de cada tipo de elemento. Los botones de reacciones usan esos mismos colores.

Los valores están en `Palette` ([LabFactory.cs](Assets/Scripts/Core/LabFactory.cs)), los paneles en [PanelMesh.cs](Assets/Scripts/Core/PanelMesh.cs) y el shader en `Assets/Laboratorio/Shaders/Panel.shader`.
