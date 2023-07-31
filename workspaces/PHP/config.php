<?php

use Dotenv\Dotenv;

$dotenv = Dotenv::createImmutable(__DIR__);
$dotenv->load();

define('CLIENT_ID', $_ENV['CLIENT_ID']);
define('CLIENT_SECRET', $_ENV['CLIENT_SECRET']);
define('NODE_ENV', $_ENV['NODE_ENV']);
$is_prod = NODE_ENV;
$paypalbase = '';
if ($is_prod == 'development')
    $paypalbase = 'https://api.sandbox.paypal.com';
else
    $paypalbase = 'https://api.paypal.com';

define('PAYPAL_API_BASE', $paypalbase);
define('WEBHOOK_ID', "");