# Energy forecast model card

## Intended use

Estimate household daily electricity usage for exploration and planning. It is not a billing, safety, grid-operations, or financial decision system.

## Model and features

Regularized multiple linear regression uses mean temperature, humidity, heating and cooling degree days, home size, occupant count, month, AC hours, and previous usage. Features are standardized using training-set statistics.

## Evaluation

Records are sorted by date and split chronologically: the earliest 80% trains the model and the latest 20% evaluates it. The app reports MAE, RMSE, and R². This is more realistic for forecasting than a random split, but it does not eliminate drift.

## Guardrails and limitations

- Fewer than 90 observations or less than six months produces an insufficient-data result.
- Synthetic-data performance does not imply real-world accuracy.
- Weather forecasts, behavioral changes, missing records, new appliances, and rate changes can reduce accuracy.
- Predictions are estimates and should always be shown with provenance and evaluation context.
