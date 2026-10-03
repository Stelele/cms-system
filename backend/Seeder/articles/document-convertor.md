# document-convertor

An AWS Lambda that was meant to convert uploaded documents, deployed through Terraform. It validates the request and stops there — the conversion was never written.

## What it does

A POST through an API Gateway HTTP API reaches `src/convertor.js`, which parses the JSON body and checks that `filename`, `base64File` and `convertTo` are all present (`validateRequest` in `src/helpers.js`). If anything is missing it returns 400 with the error message; otherwise it returns `200` with the literal string `"Hello World !!"`. The `convertTo` field is read for validation and then discarded. There is no code path that converts a document, writes output to S3, or returns a converted file.

## How it works

The deployable unit is two files: `src/convertor.js` (15 lines) exports `handler(event)`, and `src/helpers.js` (57 lines) holds three functions — `validateRequest`, `sendResponse`, and `saveTempFileLocally`. `saveTempFileLocally` sanitises `/` and `\` out of a filename and writes the base64 payload to `/tmp`, returning `false` on an empty name or a write failure. It is never called from the handler: the only references to it are in `tests/unit/saveTempFileLocally.spec.js`.

Infrastructure is Terraform. Root `start.tf` pins AWS provider 4.51.0 and Terraform 1.3.7, points at Terraform Cloud workspace `document-convetor-aws` (typo in the name), and calls a local module `./infrastructure`. Inside that module: `apigateway.tf` creates an `aws_apigatewayv2_api` with an auto-deploy stage logging to CloudWatch, a `AWS_PROXY` integration and a POST route; `lambda.tf` deploys `DocumentConvertor` with `handler = "convertor.handler"` and runtime `nodejs12.x`; `s3.tf` holds the deployment zip and a bucket; `iam.tf` attaches the execution role; `archive.tf` zips `src/` excluding `node_modules` and `tests`; `cloudwatch.tf` sets log retention to one day. `startvars.tf` (5 lines) carries the region variable.

`package.json` declares no runtime dependencies at all — only devDependencies (jest 29, eslint 8 with the standard config and a jest plugin). Scripts are `test`, `test:live`, `lint`, `lint-fix`.

### Stack

- **Node.js on AWS Lambda (`nodejs12.x`)** — the handler runtime.
- **Terraform 1.3.7 + hashicorp/aws 4.51.0** — API Gateway v2, Lambda, S3, IAM, CloudWatch, via a local `infrastructure/` module and Terraform Cloud.
- **Jest 29 + ESLint (standard)** — unit tests and linting.

## What works well

- The Terraform is complete and internally consistent: the zip archive, IAM policy, API Gateway integration and Lambda permission all reference each other correctly, so `terraform apply` would likely produce a working deployment.
- `saveTempFileLocally.spec.js` (124 lines) genuinely exercises path traversal — it tests `../`, absolute paths, backslashes and empty filenames rather than only the happy path.
- The API Gateway stage logs `integrationErrorMessage` and `requestId`, which is what you'd want when debugging a 502 from Lambda.

## What I'd change

- **The conversion does not exist.** `src/convertor.js` is 15 lines and ends in `sendResponse(200, 'Hello World !!')`. The whole premise of the repo is unimplemented; `saveTempFileLocally` is the only piece of real work and it is dead code relative to the entry point.
- `infrastructure/lambda.tf:8` pins `runtime = "nodejs12.x"`, which AWS has end-of-lifed — deploying this today would be rejected or force a runtime bump.
- The handler itself has no tests. The three spec files cover `helpers.js` only; `grep -rn "convertor" tests/` returns nothing, so the request-validation branch in the entry point is untested.
- `tests/unit/sendResponse.spec.js` is a single 5-line test asserting the return is an object — effectively no coverage of status codes or body shape.

## Still outstanding

- Actual document conversion: decode `base64File`, convert according to `convertTo`, store or return the result.
- Wiring `saveTempFileLocally` into the handler (or deleting it).
- README is 20 bytes — one heading, no usage, no deploy instructions.
- No CI workflow; `.github/` does not exist, so tests and lint are manual (`yarn test`, `yarn lint`).
- `s3.tf` bucket name `s3-document-convertor-bucket` is globally-nameable and hardcoded, which will collide or need regeneration per account.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
