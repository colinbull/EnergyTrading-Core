﻿namespace EnergyTrading.UnitTest.Xml.Serialization
{
    using EnergyTrading.Xml.Serialization;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XmSerializerExtensionsFixture : Fixture
    {
        [TestMethod]
        public void RoundTripFileXmlSerialization()
        {
            var animal = new Animal { Id = 1, Name = "Fido", Tricks = "Bark" };

            var fileName = ".\\fido.xml";
            animal.XmlSerialize(fileName);

            var candidate = fileName.LoadXmlDocument<Animal>();

            this.Check(animal, candidate);
        }

        [TestMethod]
        public void RoundTripStringXmlSerialization()
        {
            var animal = new Animal { Id = 1, Name = "Fido", Tricks = "Bark" };

            var xml = animal.XmlSerialize();

            var candidate = xml.XmlDeserializer<Animal>();

            this.Check(animal, candidate);
        }

        [TestMethod]
        public void RoundTripFileDataContractSerialization()
        {
            var animal = new Animal { Id = 1, Name = "Fido", Tricks = "Bark" };

            var fileName = ".\\fido.xml";
            animal.DataContractSerialize(fileName);

            var candidate = fileName.DeserializeDataContractXml<Animal>();

            this.Check(animal, candidate);
        }

        [TestMethod]
        public void RoundTripStringDataContractSerialization()
        {
            var animal = new Animal { Id = 1, Name = "Fido", Tricks = "Bark" };

            var xml = animal.DataContractSerialize();

            var candidate = xml.DeserializeDataContractXmlString<Animal>();

            this.Check(animal, candidate);
        }

        [TestMethod]
        public void RoundTripDataContractSerializationWithoutGeneric()
        {
            var animal = new Animal { Id = 1, Name = "Fido", Tricks = "Bark" };

            var xml = animal.DataContractSerialize();

            var candidate = xml.DeserializeDataContractXmlString(animal.GetType()) as Animal;

            this.Check(animal, candidate);
        }
    }
}