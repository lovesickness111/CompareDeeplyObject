# CompareDeeplyObject
Contains 2 files compare-object.ts and CompareObject.cs for compare object changes
# Usage of compare-object.ts in TypeScript/ JavaScript
 * @param currentObject: random object or array like {"name": "Cuong", "age": 26}
 * @param expectObject: random object or array like {"name": "Viet"}
 * @param ignoreKeys: Array of ignore keys
 * @returns:  {"name": {"current":"Cuong", "expect":"Viet"}, "age": {"current":26, "expect": null}}
# Usage of CompareObject.cs in C#
 [Test]
        public void Test_CompareObjects_ReturnOneDifferent()
        {
            // Arr
            var currentObject = new
            {
                Name = "John",
                Age = 30,
                Address = new
                {
                    City = "New York",
                    ZipCode = "10001"
                }
            };

            var expectObject = new
            {
                Name = "John2",
                Age = 30,
                Address = new
                {
                    City = "New York",
                    ZipCode = "10001"
                }
            };

            // Act
            var differences = ObjectComparer.CompareObjects(currentObject, expectObject);
            // Assert
            Assert.AreEqual(1, differences.Count);
            Assert.AreEqual(differences["Name"].Expect, "John2");

        }
