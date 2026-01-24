# Last-Furrow
Jogo de cartas e estratégia em desenvolvimento na Unity, com foco em arquitetura de sistemas, modularidade e código sustentável.

## Visão Geral
Last-Furrow é um jogo single-player de posicionamento tático e gerenciamento de recursos. O foco principal deste projeto é a implementação de uma arquitetura robusta e escalável, priorizando a separação entre as regras de negócio (Domain) e a camada de apresentação (View), sem depender excessivamente de managers monolíticos.

## Objetivos Técnicos
Este projeto serve como demonstração prática de conceitos de engenharia de software aplicados a jogos:
- **Arquitetura Modular**: Uso de Injeção de Dependência e Service Locator para desacoplar sistemas.
- **Separação Lógica/Visual**: Regras de jogo implementadas em classes puras (C#), independentes da Unity Engine.
- **Gerenciamento de Estado**: Controle de fluxo de jogo centralizado e determinístico.
- **Clean Architecture**: Aplicação de princípios SOLID, especialmente Responsabilidade Única (SRP) e Inversão de Dependência (DIP).

## Sistemas Principais
### App Modules & Bootstrapper
Sistema central de inicialização que garante ordem determinística de carregamento de dependências e elimina Race Conditions comuns no `Start/Awake`.

### Grid System
Responsável exclusivamente pela estrutura de dados do tabuleiro, validação de coordenadas e notificação de eventos de estado, totalmente desacoplado da visualização de tiles.

### Economy Service
Gerencia todas as transações, recursos do jogador e validações de compra/venda operando sobre interfaces de dados somente leitura, garantindo integridade financeira.

### Pattern Detection
Sistema especializado de algoritmos para identificar formações geométricas e padrões de cartas no grid dinamicamente.

### Run Manager
Controla o ciclo de vida da sessão de jogo, persistência de dados (Save/Load) e transição segura entre estados de progressão.

## Decisões de Arquitetura
### Lógica em C# Puro vs MonoBehaviours
A maior parte da lógica de domínio (Economia, Simulação de Grid, Inventário) reside em classes C# comuns. Isso permite:
- Testes unitários fora da Unity (Test Runner mais rápido).
- Código mais limpo e reutilizável.
- Redução de overhead da Engine.

### Service Registry Pattern
Em vez de Singletons globais espalhados, utiliza-se um AppCore centralizado que fornece acesso a serviços registrados. Isso evita o acoplamento rígido típico de Singletons globais, facilitando a substituição de implementações e Mocks para testes.

### Comunicação via Eventos
Sistemas como Tempo, Player Input e Progressão comunicam-se através de uma camada de eventos dedicada, garantindo que um módulo não precise conhecer a implementação interna do outro para reagir a mudanças de estado.

## Estado do Projeto
### Implementado
- Core Loop de Gameplay (Grid, Cartas, Turnos).
- Sistema de Economia funcional.
- Detecção de Padrões complexos.
- Persistência e Save System.
- Arquitetura de Câmera Dinâmica.

### Em Desenvolvimento
- Refinamento de Feedback Visual (Animações de Hover e Place).
- Meta-game e progressão entre runs.

### Futuro
- Expansão do sistema de Deck Building.

## Tecnologias
- Unity (2022+)
- C# (POO, Interfaces, Events)
- Git
