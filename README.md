# Open Payments .NET SDK

[![Status](https://img.shields.io/badge/status-active-success.svg)]()
[![GitHub Issues](https://img.shields.io/github/issues/interledger/open-payments-dotnet.svg)](https://github.com/kylelobo/open-payments-dotnet/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/interledger/open-payments-dotnet.svg)](https://github.com/interledger/open-payments-dotnet/pulls)

---

**TBD**

## 📝 Table of Contents

- [About](#about)
- [Getting Started](#getting-started)
- [Deployment](#deployment)
- [Usage](#usage)
- [Built Using](#built-using)
- [TODO](../TODO.md)
- [Contributing](../CONTRIBUTING.md)
- [Authors](#authors)
- [Acknowledgments](#acknowledgments)

## 🧐 About

The Open Payments .NET SDK is designed to empower developers in the .NET ecosystem by providing a ready-to-use, strongly typed library for integrating with the Open Payments API. It abstracts the complexities of HTTP communication, authentication, and data serialization, enabling developers to quickly build applications that can initiate payments and handle user interactions in a secure and standardized way.

By leveraging an OpenAPI-driven approach and NSwag-generated client code, the SDK ensures that all interactions with the Open Payments adhere to the latest specifications. This results in a consistent, maintainable, and testable codebase, allowing .NET developers to focus on implementing business logic rather than worrying about the intricacies of low-level API integrations.

## 🏁 Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See [deployment](#deployment) for notes on how to deploy the project on a live system.

### Prerequisites

What things you need to install the software and how to install them. First, install `swagger-cli` and `NSwag`:

```bash
npm install -g swagger-cli && \
dotnet tool install --global NSwag.ConsoleCore
```

### Installing

A step-by-step series of examples that tell you how to get a development env running.

First, you need to generate models from the OpenAPI specs. You can generate all of them by running the command below:

```bash
make models
```

However, you can generate them one by one (please check the `Makefile` for all supported commands).

## 🔧 Running the tests

TBD

## 🎈 Usage

TBD

## ✍️ Authors

- [@golobitch](https://github.com/golobitch) - Initial work

See also the list of [contributors](https://github.com/interledger/open-payments-dotnet/contributors) who participated in this project.

## 🎉 Acknowledgments

TBD