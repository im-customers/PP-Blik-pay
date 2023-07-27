require 'rack'
require './app/controllers/index'

# Add the Rack::Static middleware to serve static files from the 'public' directory.
app = Rack::Builder.new do
    use Rack::Static, root: 'public', urls: ['/css']
  map '/' do
    run IndexController
  end
end

Rack::Handler::WEBrick.run app