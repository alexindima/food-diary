# Ai module guidelines

Own repositories, prompt cache and complete module DI. Central Infrastructure references Model only; adapters reference shared DbContext. Do not alter transaction boundaries, lock order, retry strategy or provider compensation. Exclude Model sources from this project. Keep quota-orphan metrics on the existing shared meter via internal friend access.
