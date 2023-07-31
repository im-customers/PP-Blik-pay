<?php

namespace PaypalDemo;
use FFI\Exception;

error_reporting(0);

use \Psr\Http\Message\ServerRequestInterface as Request;
use \Psr\Http\Message\ResponseInterface as Response;
use GuzzleHttp\Client;

require 'vendor/autoload.php';
require_once __DIR__ . '/config.php';
require_once __DIR__ . '/oauth.php';
$app = new \Slim\App;


$app->get('/', function (Request $request, Response $response, array $args) {
    return $response->write(file_get_contents(__DIR__ . '/client/index.html'));
});

$app->post('/capture/{orderId}', function (Request $request, Response $response, array $args) {
    $orderId = $args['orderId'];
    $accessToken = get_access_token();

    $captureUrl = PAYPAL_API_BASE . "/v2/checkout/orders/" . $orderId . "/capture";

    $client = new Client();
    try {
        $apiResponse = $client->post($captureUrl, [
            'headers' => [
                'Authorization' => 'Bearer ' . $accessToken,
                'Content-Type' => 'application/json',
            ] // If you need to send any JSON data in the request body, include it here
        ]);
        $data = json_decode($response->getBody(), true);
        echo "💰 Payment captured!";
        return $response->withJson($data);
        // Process the API response as needed
        if ($apiResponse->getStatusCode() === 201) {
            $responseData = json_decode($apiResponse->getBody(), true);
            return $responseData;
        } else {
            return "Failed";
        }
    } catch (Exception $e) {
        echo "❌ Payment failed.";
        return $response->withStatus(400);
    }
});

/**
 * Webhook handler.
 */
$app->post('/webhook', function (Request $request, Response $response, array $args) {
    $accessToken = getAccessToken();

    $data = $request->getParsedBody();
    $eventType = $data['event_type'];
    $resource = $data['resource'];
    $orderId = $resource['id'];

    echo "🪝 Received Webhook Event";
    $client = new Client();
    $captureUrl = PAYPAL_API_BASE . "/v1/notifications/verify-webhook-signature";

    // Verify the webhook signature
    try {
        $response = $client->post($captureUrl, [
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
                'webhook_id' => WEBHOOK_ID,
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
            $response = $client->post($captureUrl, [
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