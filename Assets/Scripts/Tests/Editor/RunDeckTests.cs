using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Testes para o sistema RunDeck.
/// Verificam determinismo, filtros e comportamento geral.
/// </summary>
public class RunDeckTests
{
    // ===== DETERMINISM TESTS =====

    [Test]
    public void SameSeed_ProducesSameSequence()
    {
        // Arrange
        var drops = CreateTestDrops();
        var random1 = new SeededRandomProvider(12345);
        var random2 = new SeededRandomProvider(12345);
        var testRun = CreateTestRunData();

        // Act
        var deck1 = new RunDeckBuilder(random1, drops).Build(testRun);
        var deck2 = new RunDeckBuilder(random2, drops).Build(testRun);

        // Assert - mesma seed deve produzir mesma sequência
        Assert.AreEqual(deck1.TotalSize, deck2.TotalSize, "Decks devem ter mesmo tamanho");
        
        for (int i = 0; i < 10 && deck1.Remaining > 0; i++)
        {
            var card1 = deck1.Draw();
            var card2 = deck2.Draw();
            Assert.AreEqual(card1, card2, $"Carta {i} deve ser igual em ambos os decks");
        }
    }

    [Test]
    public void DifferentSeed_ProducesDifferentSequence()
    {
        // Arrange
        var drops = CreateTestDrops();
        var random1 = new SeededRandomProvider(12345);
        var random2 = new SeededRandomProvider(54321);
        var testRun = CreateTestRunData();

        // Act
        var deck1 = new RunDeckBuilder(random1, drops).Build(testRun);
        var deck2 = new RunDeckBuilder(random2, drops).Build(testRun);

        // Assert - seeds diferentes devem produzir sequências diferentes
        // (estatisticamente improvável que sejam iguais)
        bool foundDifference = false;
        for (int i = 0; i < 5 && deck1.Remaining > 0 && deck2.Remaining > 0; i++)
        {
            if (deck1.Draw() != deck2.Draw())
            {
                foundDifference = true;
                break;
            }
        }
        Assert.IsTrue(foundDifference, "Seeds diferentes devem produzir sequências diferentes");
    }

    // ===== DECK BEHAVIOR TESTS =====

    [Test]
    public void Draw_ReturnsCardsInOrder()
    {
        // Arrange
        var cards = new List<CardID>
        {
            (CardID)"card_a",
            (CardID)"card_b",
            (CardID)"card_c"
        };
        var deck = new RunDeck(cards);

        // Act & Assert
        Assert.AreEqual((CardID)"card_a", deck.Draw());
        Assert.AreEqual((CardID)"card_b", deck.Draw());
        Assert.AreEqual((CardID)"card_c", deck.Draw());
    }

    [Test]
    public void Draw_ReturnsInvalidWhenEmpty()
    {
        // Arrange
        var deck = new RunDeck(new List<CardID>());

        // Act
        var result = deck.Draw();

        // Assert
        Assert.IsFalse(result.IsValid, "Deck vazio deve retornar CardID inválido");
    }

    [Test]
    public void Peek_DoesNotRemoveCard()
    {
        // Arrange
        var cards = new List<CardID> { (CardID)"card_test" };
        var deck = new RunDeck(cards);

        // Act
        var peeked = deck.Peek();
        var drawn = deck.Draw();

        // Assert
        Assert.AreEqual(peeked, drawn, "Peek e Draw devem retornar a mesma carta");
    }

    [Test]
    public void Remaining_DecreasesAfterDraw()
    {
        // Arrange
        var cards = new List<CardID>
        {
            (CardID)"card_a",
            (CardID)"card_b"
        };
        var deck = new RunDeck(cards);

        // Assert
        Assert.AreEqual(2, deck.Remaining);
        deck.Draw();
        Assert.AreEqual(1, deck.Remaining);
        deck.Draw();
        Assert.AreEqual(0, deck.Remaining);
    }

    // ===== SERIALIZATION TESTS =====

    [Test]
    public void Serialization_RestoresDeckState()
    {
        // Arrange
        var cards = new List<CardID>
        {
            (CardID)"card_a",
            (CardID)"card_b",
            (CardID)"card_c"
        };
        var originalDeck = new RunDeck(cards);
        originalDeck.Draw(); // Saca uma carta

        // Act
        var (serializedIDs, drawIndex) = originalDeck.GetSerializationData();
        var restoredDeck = new RunDeck(serializedIDs, drawIndex);

        // Assert
        Assert.AreEqual(originalDeck.Remaining, restoredDeck.Remaining);
        Assert.AreEqual(originalDeck.Draw(), restoredDeck.Draw());
    }

    // ===== HELPERS =====

    private List<CardDropData> CreateTestDrops()
    {
        // Criamos CardDropData fake para testes
        // Em runtime real, isso viria do ScriptableObject
        var drops = new List<CardDropData>();
        
        // Simula 3 tipos de cartas com pesos diferentes
        var drop1 = UnityEngine.ScriptableObject.CreateInstance<CardDropData>();
        drop1.CardID = (CardID)"card_corn";
        drop1.Weight = 3;
        drop1.Rarity = CardRarity.Common;
        drop1.MaxCopiesInDeck = 5;
        drops.Add(drop1);

        var drop2 = UnityEngine.ScriptableObject.CreateInstance<CardDropData>();
        drop2.CardID = (CardID)"card_water";
        drop2.Weight = 2;
        drop2.Rarity = CardRarity.Common;
        drop2.MaxCopiesInDeck = 5;
        drops.Add(drop2);

        var drop3 = UnityEngine.ScriptableObject.CreateInstance<CardDropData>();
        drop3.CardID = (CardID)"card_rare";
        drop3.Weight = 1;
        drop3.Rarity = CardRarity.Rare;
        drop3.MaxCopiesInDeck = 1;
        drops.Add(drop3);

        return drops;
    }

    private RunData CreateTestRunData()
    {
        return new RunData
        {
            CurrentDay = 1,
            CurrentWeek = 1,
            MasterSeed = 12345
        };
    }
}
