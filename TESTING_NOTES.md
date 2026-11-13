# Testing Notes

1. **Total tests, passing, failing:** Total tests: 28, Passing: 28, Failing: 0
2. **Line coverage %:** Domain: 100%, Application: 95.8%, Total Line Coverage: 90.5% (Extracted from `coverage-report/Summary.txt`).
3. **Moq, AutoFixture and Bogus usage:** 
   - **Moq:** Used to mock the `IOrderRepository` and `IInventoryService` interfaces to isolate the `CheckoutService` during unit tests.
   - **AutoFixture:** Used to automatically generate object instances and populate test data properties without manual boilerplate instantiation.
   - **Bogus:** Used to generate realistic fake data (such as names, prices, and quantities) for testing edge cases and robust inputs.
4. **Examples B and C fairness check:** It highlights a business logic edge case where a slight price difference near a discount threshold results in a higher final payment, which is mathematically sound based on the strict rule criteria though counterintuitive for the customer.
5. **Bugs found:** None.