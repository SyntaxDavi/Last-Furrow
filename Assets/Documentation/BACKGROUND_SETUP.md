# ?? AMBIENTE TOP-DOWN

## ?? Resolução dos Sprites

**Chão (Ground)**: 16x16px ou 32x32px (tileable)
**Props (Pedras/Árvores)**: 16x16px, 32x32px ou 32x48px

**Import Settings**:
- PPU: 32
- Filter Mode: **Point**
- Compression: None

---

## ??? Setup Unity

### 1. Criar Sorting Layers
Edit ? Project Settings ? Tags and Layers:
```
0. Default
1. Background    ? (chão + props atrás)
2. Grid
3. UI
```

### 2. Setup na Cena

1. **Criar GameObject**: `Environment`
2. **Add Component**: `BackgroundController`
3. **Atribuir**:
   - Ground Sprite: seu sprite de chão tileable
   - Ground Size: `12, 8` (ajuste conforme necessário)
   - Pixel Config: arrastar ScriptableObject do projeto

### 3. Adicionar Props

Na lista **Props**:
- **Name**: "Pedra01"
- **Sprite**: sprite da pedra
- **Position**: `(-4, 2, 0)` (ajustar no Gizmo)
- **In Front Of Grid**: ? (atrás do grid)

Repita para cada objeto (árvores, pedras, arbustos).

---

## ?? Fluxo de Arte

### Chão
1. Aseprite: crie 16x16px de grama/terra
2. Export como PNG
3. Import no Unity (PPU=32, Point filter)

### Props (Pedra, Árvore, Arbusto)
1. Aseprite: crie 16x16 ou 32x32px
2. Export individual
3. Adicione na lista de Props no Inspector
4. Ajuste posição usando o Gizmo (Scene view)

---

## ?? Dica: Gizmos

Com `Environment` selecionado:
- **Verde**: área do chão
- **Ciano**: props atrás do grid
- **Amarelo**: props na frente do grid

Arraste os valores X/Y no Inspector e veja a posição em tempo real!

---

Pronto! ??
