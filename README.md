## SharpWorker
[![](https://img.shields.io/github/license/falahati/SharpWorker.svg?style=flat-square)](https://github.com/falahati/SharpWorker/blob/master/LICENSE)
[![](https://img.shields.io/github/commit-activity/y/falahati/SharpWorker.svg?style=flat-square)](https://github.com/falahati/SharpWorker/commits/master)
[![](https://img.shields.io/github/issues/falahati/SharpWorker.svg?style=flat-square)](https://github.com/falahati/SharpWorker/issues)

**SharpWorker** is a multi-platform execution environment and helper library for scheduled, API controlled tasks.

This project is licensed under LGPL and therefore can be used in closed source or commercial projects. However any commit or change to the main code must be public and there should be a read me file along with the library clarifying this as part of your project. [Read more about LGPL](https://github.com/falahati/SharpWorker/blob/master/LICENSE).

## Possible Workers
* Web Crawlers
* Data Miners
* Mini web services
* Chat bots or trade bots
* Monitoring solutions

## Features

* NetStandard2 compatible
* LiteDB database access for workers (can be replaced by writing a driver)
* Central worker configuration object for workers (saved as a json file)
* Allows workers to define public and private API endpoints with JWT based role sensitive authentication self-hosted with Owin
* Swagger support along with Swagger UI (can be disabled)
* Scheduled database ZIP backups (can be replaced by writing a backup archiver)
* Scheduled server health history and monitoring (can be disabled)
* Built-in central Logging system
* Built-in Task Scheduling system for workers
* Remote worker status reporting and control
* Accompanied with a .Net4.6.2 coordinator by default; compatible with Mono (can be replaced with a custom coordinator for the library)
* Lazy loading and execution of worker libraries

## Donation
Donations assist development and are greatly appreciated; also always remember that [every coffee counts!](https://media.makeameme.org/created/one-simply-does-i9k8kx.jpg) :)

[![](https://img.shields.io/badge/crypto-CoinPayments-8a00a3.svg?style=flat-square)](https://www.coinpayments.net/index.php?cmd=_donate&reset=1&merchant=820707aded07845511b841f9c4c335cd&item_name=Donate&currency=USD&amountf=20.00000000&allow_amount=1&want_shipping=0&allow_extra=1)
[![](https://img.shields.io/badge/shetab-ZarinPal-8a00a3.svg?style=flat-square)](https://zarinp.al/@falahati)

**--OR--**

You can always donate your time by contributing to the project or by introducing it to others.


## License
Copyright (C) 2019 Soroush Falahati

Released under the GNU Lesser General Public License v3 ("LGPLv3")