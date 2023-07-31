# app.rb
require 'sinatra'
require 'launchy'

# Define a basic route
get '/' do
  erb :index
end

# Define additional routes
get '/about' do
  'This is the About page.'
end

get '/contact' do
  'Contact us at contact@example.com'
end

# If you want to run the server directly from this file
if __FILE__ == $0
  port = ENV['PORT'] || 4567
  puts "Starting server on port #{port}..."
  
  # Open the default web browser
  Launchy.open("http://localhost:#{port}/")

  set :port, port
  #run Sinatra::Application
end
