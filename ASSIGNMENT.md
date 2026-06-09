# QA Automation Assignment

## Context

You've joined a team that owns a betting service. The service handles bet placement,
market management (open/suspend/resume), and settlement of bets based on event results.
A test suite exists.

Your job is to ensure this service is well-tested.

## Running the Service

### Option 1: .NET SDK (.NET 8+)

```bash
cd src/BettingService
dotnet run
```

Service starts at http://localhost:5000
Swagger UI at http://localhost:5000/swagger

### Option 2: Docker

```bash
docker-compose up
```

Same URLs as above.

## Your Task

Deliver a test suite that gives the team confidence the specs in `docs/test-cases/`
are properly verified. You may modify existing tests, add new ones, or both.

If you find any bugs in the service, report them.

## Technical Notes

- The existing test suite uses C#/NUnit/Playwright. We'd love to see your new tests
  in the same stack. If you're more comfortable in TypeScript/Playwright — go for it.
  We care about your testing thinking more than the language. That said, our team
  works primarily in .NET, so C# will be your day-to-day.
- The service processes some operations asynchronously. State transitions may not
  be immediate.
- API documentation is available at `/swagger` and in `docs/api-spec.yaml`.
- The test project is in `tests/BettingService.Tests/`. Run with `dotnet test`.

## What We're Looking For

- Tests that catch real business-risk bugs
- Clear reasoning about what could go wrong in production
- Evidence that you understand the specs, not just the code

## Submission

You have 48 hours. Push your work to this repo and open a PR from your working
branch into `main`. Share repo access with the reviewers listed in the email.

## Questions?

If anything is unclear about the service behavior or the assignment scope,
reach out via the email thread. Asking questions is not penalized — it's encouraged.
