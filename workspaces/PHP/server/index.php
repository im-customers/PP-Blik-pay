<?php
require 'vendor/autoload.php'; // Assuming you have installed necessary packages using Composer.
use GuzzleHttp\Client;

$dotenv = Dotenv\Dotenv::createImmutable(__DIR__);
$dotenv->load();

$paypalApiBase = "https://api.paypal.com"; // Update this with the correct PayPal API base URL.
$webhookId = "YOUR_WEBHOOK_ID"; // Replace this with your actual PayPal webhook ID.

$app = \Slim\Factory\AppFactory::create();
$app->addRoutingMiddleware();
$app->addBodyParsingMiddleware();

$app->get('/', function ($request, $response) {
  return $response->write(file_get_contents(__DIR__ . '/../client/index.html'));
});

/**
 * Capture Order handler.
 */
$app->post('/capture/{orderId}', function ($request, $response, $args) use ($paypalApiBase) {
  $orderId = $args['orderId'];

  $client = new Client();

  $accessToken = getAccessToken();

  try {
    $response = $client->post("$paypalApiBase/v2/checkout/orders/$orderId/capture", [
      'headers' => [
        'Content-Type' => 'application/json',
        'Accept' => 'application/json',
        'Authorization' => "Bearer $accessToken",
      ],
    ]);

    $data = json_decode($response->getBody(), true);
    echo "💰 Payment captured!";
    return $response->withJson($data);
  } catch (Exception $e) {
    echo "❌ Payment failed.";
    return $response->withStatus(400);
  }
});

/**
 * Webhook handler.
 */
$app->post('/webhook', function ($request, $response) use ($paypalApiBase, $webhookId) {
  $accessToken = getAccessToken();

  $data = $request->getParsedBody();
  $eventType = $data['event_type'];
  $resource = $data['resource'];
  $orderId = $resource['id'];

  echo "🪝 Received Webhook Event";

  // Verify the webhook signature
  try {
    $response = $client->post("$paypalApiBase/v1/notifications/verify-webhook-signature", [
      'headers' => [
        'Content-Type' => 'application/json',
        'Accept' => 'application/json',
        'Authorization' => "Bearer $accessToken",
      ],
      'json' => [
        'transmission_id' => $_SERVER['HTTP_PAYPAL_TRANSMISSION_ID'],
        'transmission_time' => $_SERVER['HTTP_PAYPAL_TRANSMISSION_TIME'],
        'cert_url' => $_SERVER['HTTP_PAYPAL_CERT_URL'],
        'auth_algo' => $_SERVER['HTTP_PAYPAL_AUTH_ALGO'],
        'transmission_sig' => $_SERVER['HTTP_PAYPAL_TRANSMISSION_SIG'],
        'webhook_id' => $webhookId,
        'webhook_event' => $data,
      ],
    ]);

    $responseData = json_decode($response->getBody(), true);
    $verificationStatus = $responseData['verification_status'];

    if ($verificationStatus !== 'SUCCESS') {
      echo "⚠️ Webhook signature verification failed.";
      return $response->withStatus(400);
    }
  } catch (Exception $e) {
    echo "⚠️ Webhook signature verification failed.";
    return $response->withStatus(400);
  }

  // Capture the order
  if ($eventType === 'CHECKOUT.ORDER.APPROVED') {
    try {
      $response = $client->post("$paypalApiBase/v2/checkout/orders/$orderId/capture", [
        'headers' => [
          'Content-Type' => 'application/json',
          'Accept' => 'application/json',
          'Authorization' => "Bearer $accessToken",
        ],
      ]);

      echo "💰 Payment captured!";
    } catch (Exception $e) {
      echo "❌ Payment failed.";
      return $response->withStatus(400);
    }
  }

  return $response->withStatus(200);
});

$app->run();