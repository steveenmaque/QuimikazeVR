# Tipografía de Quimikaze

| Uso | Fuente | Por qué |
|---|---|---|
| Títulos, botones y cifras | **Fredoka SemiBold** | Redondeada y amigable, como los juegos VR para todo público. Los títulos de las pantallas van en una cinta de color ([COLORES.md](COLORES.md)), y los demás títulos grandes y claros llevan un halo de neón de su color. |
| Textos | **Atkinson Hyperlegible Next Medium** | Está diseñada para que las letras parecidas no se confundan. Usa un peso medio, porque en VR los pesos finos se leen peor. |
| Símbolos y fórmulas | **Atkinson Hyperlegible Next Bold** | Distingue `l`, `I` y `1`, y también `O` y `0`. Así no se confunden Cl, I o H₂O. |

Las dos fuentes son gratuitas, con licencia SIL Open Font License.

## Qué dice la investigación
- **En VR, mejor sin pesos finos.** Los pesos finos se leen mal, el medio funciona mejor de cerca y el negrita de lejos ([Zhang, Sakamoto y Ono, 2025](https://doi.org/10.3389/frvir.2025.1590871)). Meta recomienda lo mismo para Quest: pesos más gruesos y letras de altura x grande ([Meta](https://developers.meta.com/horizon/design/styles_typography/)).
- **El grosor ayuda más cuando la letra se ve pequeña.** Es el caso de un celular dentro de un visor. Los extremos, muy fino o muy grueso, empeoran el reconocimiento ([Beier y Oderkerk, 2019](https://doi.org/10.1016/j.actpsy.2019.102904)).
- **Que las letras no se parezcan reduce errores.** Diferenciar las letras que se confunden con frecuencia mejora su reconocimiento ([Beier y Larson, 2010](https://doi.org/10.1075/idj.18.2.03bei)). Esa es la idea de Atkinson Hyperlegible ([Braille Institute](https://www.brailleinstitute.org/freefont/)).
- **Para niños: letra más grande, fuentes sans serif como Verdana y más espacio.**
  - Una letra más grande subió la edad lectora medida, y Verdana se leyó más rápido que una fuente "infantil" ([Wilkins et al., 2009](https://doi.org/10.1111/j.1467-9817.2009.01402.x)).
  - Un espaciado amplio entre letras ayuda a los niños de 8 a 14 años con dislexia ([Zorzi et al., 2012](https://doi.org/10.1073/pnas.1205566109)). Por eso los textos llevan un poco más de espacio entre letras, palabras y líneas.
- **Tamaño, distancia y contraste importan más que la fuente.** Hay estudios con medidas de tamaño angular y contraste ([Kojić et al., 2020](https://arxiv.org/abs/2004.01545); [Dingler, Kunze y Outram, 2018](https://doi.org/10.1145/3170427.3188695)). Además, el texto inclinado 60° o más necesita ser bastante más grande para leerse ([Büttner et al., 2020](https://paper.nweon.com/?p=9031)). Por eso las fichas giran hacia el jugador.
- **La buena tipografía mejora el ánimo.** Lectores con buena tipografía mostraron mejor estado de ánimo y rindieron mejor en algunas tareas ([Larson y Picard, 2005](https://www.media.mit.edu/publications/the-aesthetics-of-reading-2/)).

## Qué tomamos de los juegos VR hechos en Unity
- **Owlchemy Labs** (*Job Simulator*, *Cosmonious High*): texto grande y con buen contraste ([Unity, 2023](https://unity.com/blog/games/developing-for-blindness-low-vision-in-vr-with-cosmonious-high)). Los textos sobre los paisajes 360° llevan sombra oscura.
- **Beat Saber**: títulos en mayúsculas con brillo de neón sobre fondo oscuro. Lo aplicamos en los títulos grandes, como el nombre del elemento, con más espacio entre letras.

## Descartadas
- **Lexend:** se promociona para mejorar la lectura, pero casi no tiene estudios independientes.
- **Fuentes "infantiles" o de fantasía en los textos:** Wilkins et al. no encontraron ventajas.
